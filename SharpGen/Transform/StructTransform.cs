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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SharpGen.CppModel;
using SharpGen.Logging;
using SharpGen.Model;

namespace SharpGen.Transform;

/// <summary>
/// Transforms a C++ struct to a C# struct.
/// </summary>
public class StructTransform : TransformBase<CsStruct, CppStruct>, ITransformer<CsStruct>, ITransformPreparer<CppStruct, CsStruct>
{
    private TypeRegistry TypeRegistry => Ioc.TypeRegistry;
    private readonly MarshalledElementFactory factory;
    private readonly NamespaceRegistry namespaceRegistry;

    public StructTransform(NamingRulesManager namingRules,
                           NamespaceRegistry namespaceRegistry,
                           MarshalledElementFactory factory,
                           Ioc ioc) : base(namingRules, ioc)
    {
        this.namespaceRegistry = namespaceRegistry;
        this.factory = factory;
        factory.RequestStructProcessing += Process;
    }

    private readonly Dictionary<Regex, string> _mapMoveStructToInner = new();

    /// <summary>
    /// Moves a C++ struct to an inner C# struct.
    /// </summary>
    /// <param name="fromStruct">From C++ struct regex query.</param>
    /// <param name="toStruct">To C# struct.</param>
    public void MoveStructToInner(string fromStruct, string toStruct)
    {
        _mapMoveStructToInner.Add(new Regex("^" + fromStruct + "$"), toStruct);
    }

    /// <summary>
    /// Prepares C++ struct for mapping. This method is creating the associated C# struct.
    /// </summary>
    /// <param name="cppStruct">The c++ struct.</param>
    /// <returns></returns>
    public override CsStruct Prepare(CppStruct cppStruct)
    {
        // Create a new C# struct
        var nameSpace = namespaceRegistry.ResolveNamespace(cppStruct);
        var csStruct = new CsStruct(cppStruct, NamingRules.Rename(cppStruct))
        {
            // IsFullyMapped to false => The structure is being mapped
            IsFullyMapped = false
        };

        // Add the C# struct to its namespace
        nameSpace.Add(csStruct);

        // Map the C++ name to the C# struct
        TypeRegistry.BindType(cppStruct.Name, csStruct, source: cppStruct.ParentInclude?.Name);
        return csStruct;
    }

    /// <summary>
    /// Maps the C++ struct to C# struct.
    /// </summary>
    /// <param name="csStruct">The c sharp struct.</param>
    public override void Process(CsStruct csStruct)
    {
        // TODO: this mapping must be robust. Current calculation for field offset is not always accurate for union.
        // TODO: need to handle align/packing correctly.

        // If a struct was already mapped, then return immediately
        // The method MapStruct can be called recursively
        if (csStruct.IsFullyMapped)
            return;

        // Set IsFullyMappy in order to avoid recursive mapping
        csStruct.IsFullyMapped = true;

        // Get the associated CppStruct and CSharpTag
        var cppStruct = (CppStruct)csStruct.CppElement;

        // If this structure need to me moved to another container, move it now
        foreach (var keyValuePair in _mapMoveStructToInner)
        {
            if (keyValuePair.Key.Match(csStruct.CppElementName).Success)
            {
                string cppName = keyValuePair.Key.Replace(csStruct.CppElementName, keyValuePair.Value);
                var destSharpStruct = (CsStruct)TypeRegistry.FindBoundType(cppName);
                // Remove the struct from his container
                csStruct.Parent.Remove(csStruct);
                // Add this struct to the new container struct
                destSharpStruct.Add(csStruct);
            }
        }

        // Current offset of a field
        uint currentFieldAbsoluteOffset = 0;

        // Last field offset
        int previousFieldOffsetIndex = -1;

        // Size of the last field
        uint previousFieldSize = 0;

        uint maxSizeOfField = 0;

        bool isNonSequential = false;

        int cumulatedBitOffset = 0;

        var inheritedStructs = new Stack<CppStruct>();
        var currentStruct = cppStruct;
        while (currentStruct != null && currentStruct.Base != currentStruct.Name)
        {
            inheritedStructs.Push(currentStruct);
            currentStruct = TypeRegistry.FindBoundType(currentStruct.Base)?.CppElement as CppStruct;
        }

        while (inheritedStructs.Count > 0)
        {
            currentStruct = inheritedStructs.Pop();

            var fields = currentStruct.Fields.ToArray();
            int fieldCount = fields.Length;
            var fieldNames = NamingRules.Rename(fields);

            // -------------------------------------------------------------------------------
            // Iterate on all fields and perform mapping
            // -------------------------------------------------------------------------------
            for (int fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
            {
                var cppField = fields[fieldIndex];
                var fieldName = fieldNames[fieldIndex];
                Logger.RunInContext(
                    cppField.ToString(),
                    () =>
                    {
                        var csField = factory.Create(cppField, fieldName);
                        csStruct.Add(csField);

                        // If last field has same offset, then it's a union
                        // CurrentOffset is not moved
                        if (isNonSequential && previousFieldOffsetIndex != cppField.Offset)
                        {
                            previousFieldSize = maxSizeOfField;
                            maxSizeOfField = 0;
                            isNonSequential = false;
                        }

                        currentFieldAbsoluteOffset += previousFieldSize;

                        // If field alignment is null, then we have a pointer somewhere so we can't align
                        if (csField.MarshalType.Alignment is { } fieldAlignment)
                        {
                            // otherwise, align the field on the alignment requirement of the field
                            var delta = currentFieldAbsoluteOffset % fieldAlignment;
                            if (delta != 0)
                                currentFieldAbsoluteOffset += fieldAlignment - delta;
                        }

                        // Get correct offset (for handling union)
                        csField.Offset = currentFieldAbsoluteOffset;

                        // Handle bit fields : calculate BitOffset and BitMask for this field
                        if (previousFieldOffsetIndex != cppField.Offset)
                            cumulatedBitOffset = 0;

                        if (cppField.IsBitField)
                        {
                            var lastCumulatedBitOffset = cumulatedBitOffset;
                            cumulatedBitOffset += cppField.BitOffset;
                            csField.BitMask = (1 << cppField.BitOffset) - 1;
                            csField.BitOffset = lastCumulatedBitOffset;
                        }

                        var nextFieldIndex = fieldIndex + 1;
                        if (previousFieldOffsetIndex == cppField.Offset
                         || nextFieldIndex < fieldCount && fields[nextFieldIndex].Offset == cppField.Offset)
                        {
                            if (previousFieldOffsetIndex != cppField.Offset)
                                maxSizeOfField = 0;

                            maxSizeOfField = Math.Max(csField.Size, maxSizeOfField);
                            isNonSequential = true;
                            csStruct.ExplicitLayout = true;
                            previousFieldSize = 0;
                        }
                        else
                        {
                            previousFieldSize = csField.Size;
                        }

                        previousFieldOffsetIndex = cppField.Offset;
                    }
                );
            }
        }

        // In case of explicit layout, check that we can safely generate it on both x86 and x64 (in case of an union
        // using pointers, we can't)
        if (!csStruct.HasCustomMarshal && csStruct.ExplicitLayout && !cppStruct.IsUnion)
        {
            var fieldList = csStruct.Fields;
            for (var i = 0; i < fieldList.Count; i++)
            {
                var field = fieldList[i];
                var fieldAlignment = field.MarshalType.Alignment;

                if (fieldAlignment.HasValue)
                    continue;

                // If pointer field is not the last one, than we can't handle it
                if (i + 1 >= fieldList.Count)
                    continue;

                Logger.Error(
                    LoggingCodes.NonPortableAlignment,
                    "The field [{0}] in structure [{1}] has pointer alignment within a structure that requires explicit layout. This situation cannot be handled on both 32-bit and 64-bit architectures. This structure needs manual layout (remove fields from definition) and write them manually in xml mapping files",
                    field.CppElementName,
                    csStruct.CppElementName
                );

                break;
            }
        }

        csStruct.StructSize = currentFieldAbsoluteOffset + previousFieldSize;
    }
}