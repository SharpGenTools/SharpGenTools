// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using Microsoft.Extensions.DependencyModel;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using CallSite = Mono.Cecil.CallSite;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace SharpPatch
{
    /// <summary>
    /// InteropApp is responsible to patch SharpGen assemblies and inject unmanaged interop call.
    /// InteropApp is also adding several useful methods:
    /// - memcpy using cpblk
    /// - Read/ReadRange/Write/WriteRange of structured data to a memory location
    /// - SizeOf on generic structures (C# usually doesn't allow this).
    /// </summary>
    public class InteropApp
    {
        private readonly List<TypeDefinition> classToRemoveList = new List<TypeDefinition>();
        AssemblyDefinition assembly;

        public IAssemblyResolver AssemblyResolver { get; set; }

        public ILogger Logger { get; set; }

        /// <summary>
        /// Patches the method.
        /// </summary>
        /// <param name="method">The method.</param>
        void PatchMethod(MethodDefinition method)
        {
            if (method.HasBody)
            {
                var ilProcessor = method.Body.GetILProcessor();

                var instructions = method.Body.Instructions;
                for (int i = 0; i < instructions.Count; i++)
                {
                    Instruction instruction = instructions[i];

                    if (instruction.OpCode == OpCodes.Call && instruction.Operand is MethodReference)
                    {
                        var methodDescription = (MethodReference)instruction.Operand;

                        if (methodDescription.Name.StartsWith("Calli") && methodDescription.DeclaringType.Name == "LocalInterop")
                        {
                            var callSite = new CallSite(methodDescription.ReturnType) { CallingConvention = MethodCallingConvention.StdCall };

                            if (methodDescription.Name.StartsWith("CalliCdecl"))
                            {
                                callSite.CallingConvention = MethodCallingConvention.C;
                            }
                            else if(methodDescription.Name.StartsWith("CalliThisCall"))
                            {
                                callSite.CallingConvention = MethodCallingConvention.ThisCall;
                            }
                            else if(methodDescription.Name.StartsWith("CalliStdCall"))
                            {
                                callSite.CallingConvention = MethodCallingConvention.StdCall;
                            }
                            else if(methodDescription.Name.StartsWith("CalliFastCall"))
                            {
                                callSite.CallingConvention = MethodCallingConvention.FastCall;
                            }

                            // Last parameter is the function ptr, so we don't add it as a parameter for calli
                            // as it is already an implicit parameter for calli
                            for (int j = 0; j < methodDescription.Parameters.Count - 1; j++)
                            {
                                var parameterDefinition = methodDescription.Parameters[j];
                                callSite.Parameters.Add(parameterDefinition);
                            }

                            // Create calli Instruction
                            var callIInstruction = ilProcessor.Create(OpCodes.Calli, callSite);

                            // Replace instruction
                            ilProcessor.Replace(instruction, callIInstruction);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Patches the type.
        /// </summary>
        /// <param name="type">The type.</param>
        void PatchType(TypeDefinition type)
        {
            // Patch methods
            foreach (var method in type.Methods)
                PatchMethod(method);

            if (type.Name == "LocalInterop")
                classToRemoveList.Add(type);

            // Patch nested types
            foreach (var typeDefinition in type.NestedTypes)
                PatchType(typeDefinition);
        }

        /// <summary>
        /// Patches the file.
        /// </summary>
        /// <param name="file">The file.</param>
        public bool PatchFile(string file)
        {
            file = Path.Combine(Directory.GetCurrentDirectory(), file);

            var fileTime = new FileTime(file);
            var checkFile = Path.GetFullPath(file) + ".check";

            // If checkFile and checkInteropBuilderFile up-to-date, then nothing to do
            if (fileTime.CheckFileUpToDate(checkFile))
            {
                Log("Nothing to do. SharpPatch patch was already applied for assembly [{0}]", file);
                return false;
            }

            // Copy PDB from input assembly to output assembly if any
            var readerParameters = new ReaderParameters
            {
                InMemory = true
            };

            try
            {
                readerParameters.AssemblyResolver = AssemblyResolver ?? CreateDepsFileAssemblyResolver(file);
            }
            catch (Exception ex)
            {
                LogError($"Unable to load assembly resolver: {ex}");
                return false;
            }

            var writerParameters = new WriterParameters();
            var pdbName = Path.ChangeExtension(file, "pdb");
            if (File.Exists(pdbName))
            {
                readerParameters.SymbolReaderProvider = GetSymbolReaderProvider(pdbName);
                readerParameters.ReadSymbols = true;
                writerParameters.WriteSymbols = true;
            }

            // Read Assembly
            assembly = AssemblyDefinition.ReadAssembly(file, readerParameters);

            Log("SharpPatch interop patch for assembly [{0}]", file);
            foreach (var type in assembly.MainModule.Types)
                PatchType(type);

            // Remove All Interop classes
            foreach (var type in classToRemoveList)
                assembly.MainModule.Types.Remove(type);

            var outputFilePath = file;
            assembly.Write(outputFilePath, writerParameters);

            fileTime = new FileTime(file);
            // Update Check file
            fileTime.UpdateCheckFile(checkFile);

            Log("SharpPatch patch done for assembly [{0}]", file);
            return true;
        }

        private static IAssemblyResolver CreateDepsFileAssemblyResolver(string file)
        {
            var depsFilePath = Path.Combine(Path.GetDirectoryName(file), $"{Path.GetFileNameWithoutExtension(file)}.deps.json");

            using (var depsFile = File.OpenRead(depsFilePath))
            using(var reader = new DependencyContextJsonReader())
            {
                var AssemblyResolver = new DependencyContextAssemblyResolver(reader.Read(depsFile), Path.GetDirectoryName(file));
                return AssemblyResolver;
            }
        }

        private static ISymbolReaderProvider GetSymbolReaderProvider(string pdbName)
        {
            using (var pdbStream = File.OpenRead(pdbName))
            using (var reader = new BinaryReader(pdbStream))
            {
                var headerBytes = reader.ReadBytes(4);
                if(Encoding.ASCII.GetString(headerBytes) == "DSJB") // Start of the portable pdb format
                {
                    return new PortablePdbReaderProvider();
                }
            }
            return new PdbReaderProvider(); // otherwise assume full pdb format
        }

        public void Log(string message, params object[] parameters)
        {
            Logger.Log(message, parameters);
        }

        public void LogError(string message, params object[] parameters)
        {
            Logger.LogError(message, parameters);
        }

    }
}
