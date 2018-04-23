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
        private TypeReference voidType;
        private TypeReference voidPointerType;
        private TypeReference intType;

        public IAssemblyResolver AssemblyResolver { get; set; }

        public string GlobalNamespace { get; set; }
        public ILogger Logger { get; set; }

        /// <summary>
        /// Creates a module init for a C# assembly.
        /// </summary>
        /// <param name="method">The method to add to the module init.</param>
        private void CreateModuleInit(MethodDefinition method)
        {
            const MethodAttributes ModuleInitAttributes = MethodAttributes.Static
                                                          | MethodAttributes.Assembly
                                                          | MethodAttributes.SpecialName
                                                          | MethodAttributes.RTSpecialName;

            var moduleType = assembly.MainModule.GetType("<Module>");

            // Get or create ModuleInit method
            var cctor = moduleType.Methods.FirstOrDefault(moduleTypeMethod => moduleTypeMethod.Name == ".cctor");
            if (cctor == null)
            {
                cctor = new MethodDefinition(".cctor", ModuleInitAttributes, method.ReturnType);
                moduleType.Methods.Add(cctor);
            }

            var isCallAlreadyDone = cctor.Body.Instructions.Any(instruction => instruction.OpCode == OpCodes.Call && instruction.Operand == method);

            // If the method is not called, we can add it
            if (!isCallAlreadyDone)
            {
                var ilProcessor = cctor.Body.GetILProcessor();
                var retInstruction = cctor.Body.Instructions.FirstOrDefault(instruction => instruction.OpCode == OpCodes.Ret);
                var callMethod = ilProcessor.Create(OpCodes.Call, method);

                if (retInstruction == null)
                {
                    // If a ret instruction is not present, add the method call and ret
                    ilProcessor.Append(callMethod);
                    ilProcessor.Emit(OpCodes.Ret);
                }
                else
                {
                    // If a ret instruction is already present, just add the method to call before
                    ilProcessor.InsertBefore(retInstruction, callMethod);
                }
            }
        }

        /// <summary>
        /// Creates the write method with the following signature:
        /// <code>
        /// public static unsafe void* Write&lt;T&gt;(void* pDest, ref T data) where T : struct
        /// </code>
        /// </summary>
        /// <param name="method">The method to patch</param>
        private void CreateWriteMethod(MethodDefinition method)
        {
            method.Body.Instructions.Clear();
            method.Body.InitLocals = true;

            var gen = method.Body.GetILProcessor();
            var paramT = method.GenericParameters[0];
            // Preparing locals
            // local(0) int
            method.Body.Variables.Add(new VariableDefinition(intType));
            // local(1) T*
            method.Body.Variables.Add(new VariableDefinition(new PinnedType(new ByReferenceType(paramT))));

            // Push (0) pDest for memcpy
            gen.Emit(OpCodes.Ldarg_0);

            // fixed (void* pinnedData = &data[offset])
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Stloc_1);

            // Push (1) pinnedData for memcpy
            gen.Emit(OpCodes.Ldloc_1);

            // totalSize = sizeof(T)
            gen.Emit(OpCodes.Sizeof, paramT);
            gen.Emit(OpCodes.Conv_I4);
            gen.Emit(OpCodes.Stloc_0);

            // Push (2) totalSize
            gen.Emit(OpCodes.Ldloc_0);

            // Emit cpblk
            EmitCpblk(method, gen);

            // Return pDest + totalSize
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Conv_I);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Add);

            // Ret
            gen.Emit(OpCodes.Ret);
        }

        private static void ReplaceFixedStatement(MethodDefinition method, ILProcessor ilProcessor, Instruction fixedtoPatch)
        {
            var paramT = ((GenericInstanceMethod)fixedtoPatch.Operand).GenericArguments[0];
            // Preparing locals
            // local(0) T*
            method.Body.Variables.Add(new VariableDefinition(new PinnedType(new ByReferenceType(paramT))));

            var index = method.Body.Variables.Count - 1;

            Instruction ldlocFixed;
            Instruction stlocFixed;
            switch (index)
            {
                case 0:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc_0);
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc_0);
                    break;
                case 1:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc_1);
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc_1);
                    break;
                case 2:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc_2);
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc_2);
                    break;
                case 3:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc_3);
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc_3);
                    break;
                default:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc, index);
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc, index);
                    break;
            }
            ilProcessor.InsertBefore(fixedtoPatch, stlocFixed);
            ilProcessor.Replace(fixedtoPatch, ldlocFixed);
        }

        private static void ReplaceReadInline(MethodDefinition method, ILProcessor ilProcessor, Instruction fixedtoPatch)
        {
            var paramT = ((GenericInstanceMethod)fixedtoPatch.Operand).GenericArguments[0];
            var copyInstruction = ilProcessor.Create(OpCodes.Ldobj, paramT);
            ilProcessor.Replace(fixedtoPatch, copyInstruction);
        }

        private static void ReplaceCopyInline(MethodDefinition method, ILProcessor ilProcessor, Instruction fixedtoPatch)
        {
            var paramT = ((GenericInstanceMethod)fixedtoPatch.Operand).GenericArguments[0];
            var copyInstruction = ilProcessor.Create(OpCodes.Cpobj, paramT);
            ilProcessor.Replace(fixedtoPatch, copyInstruction);
        }

        private static void ReplaceSizeOfStructGeneric(MethodDefinition method, ILProcessor ilProcessor, Instruction fixedtoPatch)
        {
            var paramT = ((GenericInstanceMethod)fixedtoPatch.Operand).GenericArguments[0];
            var copyInstruction = ilProcessor.Create(OpCodes.Sizeof, paramT);
            ilProcessor.Replace(fixedtoPatch, copyInstruction);
        }

        /// <summary>
        /// Creates the cast  method with the following signature:
        /// <code>
        /// public static unsafe void* Cast&lt;T&gt;(ref T data) where T : struct
        /// </code>
        /// </summary>
        /// <param name="method">The method cast.</param>
        private static void CreateCastMethod(MethodDefinition method)
        {
            method.Body.Instructions.Clear();
            method.Body.InitLocals = true;

            var gen = method.Body.GetILProcessor();

            gen.Emit(OpCodes.Ldarg_0);

            // Ret
            gen.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Creates the cast  method with the following signature:
        /// <code>
        /// public static TCAST[] CastArray&lt;TCAST, T&gt;(T[] arrayData) where T : struct where TCAST : struct
        /// </code>
        /// </summary>
        /// <param name="method">The method cast array.</param>
        private static void CreateCastArrayMethod(MethodDefinition method)
        {
            method.Body.Instructions.Clear();
            method.Body.InitLocals = true;

            var gen = method.Body.GetILProcessor();

            gen.Emit(OpCodes.Ldarg_0);

            // Ret
            gen.Emit(OpCodes.Ret);
        }

        private static void ReplaceFixedArrayStatement(MethodDefinition method, ILProcessor ilProcessor, Instruction fixedtoPatch)
        {
            var paramT = ((GenericInstanceMethod)fixedtoPatch.Operand).GenericArguments[0];
            // Preparing locals
            // local(0) T*
            method.Body.Variables.Add(new VariableDefinition(new PinnedType(new ByReferenceType(paramT))));

            var index = method.Body.Variables.Count - 1;

            Instruction ldlocFixed;
            Instruction stlocFixed;
            switch (index)
            {
                case 0:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc_0);
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc_0);
                    break;
                case 1:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc_1);
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc_1);
                    break;
                case 2:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc_2);
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc_2);
                    break;
                case 3:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc_3);
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc_3);
                    break;
                default:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc, index);
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc, index);
                    break;
            }

            var instructionLdci40 = ilProcessor.Create(OpCodes.Ldc_I4_0);
            ilProcessor.InsertBefore(fixedtoPatch, instructionLdci40);
            var instructionLdElema = ilProcessor.Create(OpCodes.Ldelema, paramT);
            ilProcessor.InsertBefore(fixedtoPatch, instructionLdElema);
            ilProcessor.InsertBefore(fixedtoPatch, stlocFixed);
            ilProcessor.Replace(fixedtoPatch, ldlocFixed);
        }

        /// <summary>
        /// Creates the write range method with the following signature:
        /// <code>
        /// public static unsafe void* Write&lt;T&gt;(void* pDest, T[] data, int offset, int count) where T : struct
        /// </code>
        /// </summary>
        /// <param name="method">The method copy struct.</param>
        private void CreateWriteRangeMethod(MethodDefinition method)
        {
            method.Body.Instructions.Clear();
            method.Body.InitLocals = true;

            var gen = method.Body.GetILProcessor();
            var paramT = method.GenericParameters[0];
            // Preparing locals
            // local(0) int
            method.Body.Variables.Add(new VariableDefinition(intType));
            // local(1) T*
            method.Body.Variables.Add(new VariableDefinition(new PinnedType(new ByReferenceType(paramT))));

            // Push (0) pDest for memcpy
            gen.Emit(OpCodes.Ldarg_0);

            // fixed (void* pinnedData = &data[offset])
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Ldarg_2);
            gen.Emit(OpCodes.Ldelema, paramT);
            gen.Emit(OpCodes.Stloc_1);

            // Push (1) pinnedData for memcpy
            gen.Emit(OpCodes.Ldloc_1);

            // totalSize = sizeof(T) * count
            gen.Emit(OpCodes.Sizeof, paramT);
            gen.Emit(OpCodes.Conv_I4);
            gen.Emit(OpCodes.Ldarg_3);
            gen.Emit(OpCodes.Mul);
            gen.Emit(OpCodes.Stloc_0);

            // Push (2) totalSize
            gen.Emit(OpCodes.Ldloc_0);

            // Emit cpblk
            EmitCpblk(method, gen);

            // Return pDest + totalSize
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Conv_I);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Add);

            // Ret
            gen.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Creates the read method with the following signature:
        /// <code>
        /// public static unsafe void* Read&lt;T&gt;(void* pSrc, ref T data) where T : struct
        /// </code>
        /// </summary>
        /// <param name="method">The method copy struct.</param>
        private void CreateReadMethod(MethodDefinition method)
        {
            method.Body.Instructions.Clear();
            method.Body.InitLocals = true;

            var gen = method.Body.GetILProcessor();
            var paramT = method.GenericParameters[0];

            // Preparing locals
            // local(0) int
            method.Body.Variables.Add(new VariableDefinition(intType));
            // local(1) T*

            method.Body.Variables.Add(new VariableDefinition(new PinnedType(new ByReferenceType(paramT))));

            // fixed (void* pinnedData = &data[offset])
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Stloc_1);

            // Push (0) pinnedData for memcpy
            gen.Emit(OpCodes.Ldloc_1);

            // Push (1) pSrc for memcpy
            gen.Emit(OpCodes.Ldarg_0);

            // totalSize = sizeof(T)
            gen.Emit(OpCodes.Sizeof, paramT);
            gen.Emit(OpCodes.Conv_I4);
            gen.Emit(OpCodes.Stloc_0);

            // Push (2) totalSize
            gen.Emit(OpCodes.Ldloc_0);

            // Emit cpblk
            EmitCpblk(method, gen);

            // Return pDest + totalSize
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Conv_I);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Add);

            // Ret
            gen.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Creates the read range method with the following signature:
        /// <code>
        /// public static unsafe void* Read&lt;T&gt;(void* pSrc, T[] data, int offset, int count) where T : struct
        /// </code>
        /// </summary>
        /// <param name="method">The method copy struct.</param>
        private void CreateReadRangeMethod(MethodDefinition method)
        {
            method.Body.Instructions.Clear();
            method.Body.InitLocals = true;

            var gen = method.Body.GetILProcessor();
            var paramT = method.GenericParameters[0];
            // Preparing locals
            // local(0) int
            method.Body.Variables.Add(new VariableDefinition(intType));
            // local(1) T*
            method.Body.Variables.Add(new VariableDefinition(new PinnedType(new ByReferenceType(paramT))));

            // fixed (void* pinnedData = &data[offset])
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Ldarg_2);
            gen.Emit(OpCodes.Ldelema, paramT);
            gen.Emit(OpCodes.Stloc_1);

            // Push (0) pinnedData for memcpy
            gen.Emit(OpCodes.Ldloc_1);

            // Push (1) pDest for memcpy
            gen.Emit(OpCodes.Ldarg_0);

            // totalSize = sizeof(T) * count
            gen.Emit(OpCodes.Sizeof, paramT);
            gen.Emit(OpCodes.Conv_I4);
            gen.Emit(OpCodes.Ldarg_3);
            gen.Emit(OpCodes.Conv_I4);
            gen.Emit(OpCodes.Mul);
            gen.Emit(OpCodes.Stloc_0);

            // Push (2) totalSize
            gen.Emit(OpCodes.Ldloc_0);

            // Emit cpblk
            EmitCpblk(method, gen);

            // Return pDest + totalSize
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Conv_I);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Add);

            // Ret
            gen.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Creates the memcpy method with the following signature:
        /// <code>
        /// public static unsafe void memcpy(void* pDest, void* pSrc, int count)
        /// </code>
        /// </summary>
        /// <param name="methodCopyStruct">The method copy struct.</param>
        private static void CreateMemcpy(MethodDefinition methodCopyStruct)
        {
            methodCopyStruct.Body.Instructions.Clear();

            var gen = methodCopyStruct.Body.GetILProcessor();

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Ldarg_2);
            gen.Emit(OpCodes.Unaligned, (byte)1);       // unaligned to 1
            gen.Emit(OpCodes.Cpblk);

            // Ret
            gen.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Creates the memset method with the following signature:
        /// <code>
        /// public static unsafe void memset(void* pDest, byte value, int count)
        /// </code>
        /// </summary>
        /// <param name="methodSetStruct">The method set struct.</param>
        private static void CreateMemset(MethodDefinition methodSetStruct)
        {
            methodSetStruct.Body.Instructions.Clear();

            var gen = methodSetStruct.Body.GetILProcessor();

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Ldarg_2);
            gen.Emit(OpCodes.Unaligned, (byte)1);       // unaligned to 1
            gen.Emit(OpCodes.Initblk);

            // Ret
            gen.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Emits the cpblk method, supporting x86 and x64 platform.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="gen">The gen.</param>
        private static void EmitCpblk(MethodDefinition method, ILProcessor gen)
        {
            var cpblk = gen.Create(OpCodes.Cpblk);
            gen.Emit(OpCodes.Unaligned, (byte)1);       // unaligned to 1
            gen.Append(cpblk);
        }

        private List<string>  GetSharpGenAttributes(MethodDefinition method)
        {
            var attributes = new List<string>();
            foreach (var customAttribute in method.CustomAttributes)
            {
                if (customAttribute.AttributeType.FullName == GlobalNamespace + ".TagAttribute")
                {
                    var value = customAttribute.ConstructorArguments[0].Value;
                    attributes.Add(value == null ? string.Empty : value.ToString());
                }
            }

            return attributes;
        }

        /// <summary>
        /// Patches the method.
        /// </summary>
        /// <param name="method">The method.</param>
        void PatchMethod(MethodDefinition method)
        {
            var attributes = GetSharpGenAttributes(method);
            if (attributes.Contains(GlobalNamespace + ".ModuleInit"))
            {
                CreateModuleInit(method);
            }

            if (method.DeclaringType.Name == "Interop")
            {
                if (method.Name == "memcpy")
                {
                    CreateMemcpy(method);
                }
                else if (method.Name == "memset")
                {
                    CreateMemset(method);
                }
                else if ((method.Name == "Cast") || (method.Name == "CastOut"))
                {
                    CreateCastMethod(method);
                }
                else if (method.Name == "CastArray")
                {
                    CreateCastArrayMethod(method);
                }
                else if (method.Name == "Read" || (method.Name == "ReadOut") || (method.Name == "Read2D"))
                {
                    if (method.Parameters.Count == 2)
                        CreateReadMethod(method);
                    else
                        CreateReadRangeMethod(method);
                }
                else if (method.Name == "Write" || (method.Name == "Write2D"))
                {
                    if (method.Parameters.Count == 2)
                        CreateWriteMethod(method);
                    else
                        CreateWriteRangeMethod(method);
                }
            }
            else if (method.HasBody)
            {
                var ilProcessor = method.Body.GetILProcessor();

                var instructions = method.Body.Instructions;
                Instruction instruction = null;
                Instruction previousInstruction;
                for (int i = 0; i < instructions.Count; i++)
                {
                    previousInstruction = instruction;
                    instruction = instructions[i];

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
                        else if (methodDescription.DeclaringType.Name == "Interop")
                        {
                            if (methodDescription.FullName.Contains("Fixed"))
                            {
                                if (methodDescription.Parameters[0].ParameterType.IsArray)
                                {
                                    ReplaceFixedArrayStatement(method, ilProcessor, instruction);
                                }
                                else
                                {
                                    ReplaceFixedStatement(method, ilProcessor, instruction);
                                }
                            }
                            else if (methodDescription.Name.StartsWith("ReadInline"))
                            {
                                ReplaceReadInline(method, ilProcessor, instruction);
                            }
                            else if (methodDescription.Name.StartsWith("CopyInline") || methodDescription.Name.StartsWith("WriteInline"))
                            {
                                ReplaceCopyInline(method, ilProcessor, instruction);
                            }
                            else if (methodDescription.Name.StartsWith("SizeOf"))
                            {
                                ReplaceSizeOfStructGeneric(method, ilProcessor, instruction);
                            }
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

            // Import void* and int32
            voidType = assembly.MainModule.TypeSystem.Void.Resolve();
            voidPointerType = new PointerType(assembly.MainModule.ImportReference(voidType));
            intType = assembly.MainModule.ImportReference(assembly.MainModule.TypeSystem.Int32.Resolve());

            // Remove CompilationRelaxationsAttribute
            for (int i = 0; i < assembly.CustomAttributes.Count; i++)
            {
                var customAttribute = assembly.CustomAttributes[i];
                if (customAttribute.AttributeType.FullName == typeof(CompilationRelaxationsAttribute).FullName)
                {
                    assembly.CustomAttributes.RemoveAt(i);
                    i--;
                }
            }

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
