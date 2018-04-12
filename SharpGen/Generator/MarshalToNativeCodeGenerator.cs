using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using Microsoft.CodeAnalysis.CSharp;

namespace SharpGen.Generator
{
    /// <summary>
    /// Generates code to marshal data for a parameter/field/return value to its native representation.
    /// </summary>
    class MarshalToNativeCodeGenerator : MarshallingCodeGeneratorBase, ICodeGenerator<CsMarshalBase, StatementSyntax>
    {
        private readonly GlobalNamespaceProvider globalNamespace;
        private readonly bool singleStackFrame;
        private bool MarshalPinnableElements => !singleStackFrame;

        public MarshalToNativeCodeGenerator(bool singleStackFrame, GlobalNamespaceProvider globalNamespace)
            :base(globalNamespace)
        {
            this.singleStackFrame = singleStackFrame;
            this.globalNamespace = globalNamespace;
        }

        public StatementSyntax GenerateCode(CsMarshalBase csElement)
        {
            if (csElement.IsInterfaceArray) // Interface arrays don't need any special marshalling.
            {
                return null;
            }

            if (csElement.IsArray)
            {
                if (csElement.HasNativeValueType)
                {
                    return LoopThroughArrayParameter(
                        csElement,
                        (publicElement, marshalElement) =>
                            CreateMarshalStructStatement(
                                csElement,
                                "__MarshalTo",
                                publicElement,
                                marshalElement));
                }
                else if (csElement.IsInterface)
                {
                    return LoopThroughArrayParameter(
                        csElement,
                        (publicElement, marshalElement) =>
                            MarshalInterfaceInstanceToNative(csElement, publicElement, marshalElement));
                }
                else if (csElement.IsBoolToInt)
                {
                    return NotImplemented("Bool to int arrays");
                }
                else if (csElement.IsString) // Character array presented to the user as a string.
                {
                    if (!csElement.IsWideChar)
                    {
                        return GenerateAnsiStringToArray(csElement);
                    }
                    else if (MarshalPinnableElements)
                    {
                        return GenerateStringToArray(csElement);
                    }

                    return null;
                }
                else if (csElement.IsValueType)
                {
                    if (MarshalPinnableElements)
                    {
                        return GenerateCopyMemory(csElement, copyFromNative: false); 
                    }
                    return null;
                }
                throw new ArgumentException($"Missing array marshalling to native for {csElement}.", nameof(csElement));
            }
            else if (csElement.IsString)
            {
                if (!csElement.IsWideChar || MarshalPinnableElements)
                {
                    return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                    GetMarshalStorageLocation(csElement),
                                    InvocationExpression(
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            globalNamespace.GetTypeNameSyntax(BuiltinType.Marshal),
                                            IdentifierName("StringToHGlobal" + (csElement.IsWideChar ? "Uni" : "Ansi"))),
                                        ArgumentList(SingletonSeparatedList(
                                            Argument(
                                                IdentifierName(csElement.Name)))))));
                }
                return null;
            }
            else if (csElement.HasNativeValueType)
            {
                ExpressionSyntax publicElementExpression = IdentifierName(csElement.Name);

                if (csElement.IsNullableStruct)
                {
                    publicElementExpression = MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        publicElementExpression,
                        IdentifierName("Value"));
                }
                return CreateMarshalStructStatement(
                    csElement,
                    "__MarshalTo",
                    publicElementExpression,
                    GetMarshalStorageLocation(csElement));
            }
            else if (csElement.IsInterface)
            {
                return MarshalInterfaceInstanceToNative(
                   csElement,
                   IdentifierName(csElement.Name),
                   GetMarshalStorageLocation(csElement));
            }
            else if (csElement.IsNullableStruct)
            {
                return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    GetMarshalStorageLocation(csElement),
                    BinaryExpression(SyntaxKind.CoalesceExpression,
                        IdentifierName(csElement.Name),
                        DefaultExpression(ParseTypeName(csElement.PublicType.QualifiedName)))));
            }
            else if (csElement.MarshalType != csElement.PublicType
                && !csElement.IsBoolToInt
                && !(csElement.MarshalType.QualifiedName == "System.IntPtr" && csElement.HasPointer))
            {
                return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        GetMarshalStorageLocation(csElement),
                        CastExpression(ParseTypeName(csElement.MarshalType.QualifiedName),
                            IdentifierName(csElement.Name))));
            }
            else if (csElement is CsField field) // Fields are always marshalled
            {
                if (csElement.IsBoolToInt) // integer value cached in intermediate field.
                {
                    return ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        GetMarshalStorageLocation(csElement),
                        IdentifierName(csElement.IntermediateMarshalName)));
                }
                else if (field.IsBitField)
                {
                    return ExpressionStatement(
                        AssignmentExpression(SyntaxKind.OrAssignmentExpression,
                        GetMarshalStorageLocation(csElement),
                        CastExpression(ParseTypeName(csElement.MarshalType.QualifiedName),
                            ParenthesizedExpression(BinaryExpression(SyntaxKind.BitwiseAndExpression,
                                IdentifierName(csElement.IntermediateMarshalName),
                                LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    Literal(field.BitMask << field.BitOffset)))))));
                }
                return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        GetMarshalStorageLocation(csElement),
                        IdentifierName(csElement.Name)));
            }

            return null;
        }

        private ExpressionStatementSyntax MarshalInterfaceInstanceToNative(CsMarshalBase csElement, ExpressionSyntax publicElement, ExpressionSyntax marshalElement)
        {
            return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    marshalElement,
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            globalNamespace.GetTypeNameSyntax(WellKnownName.CppObject),
                            GenericName(
                                Identifier("ToCallbackPtr"))
                            .WithTypeArgumentList(
                                TypeArgumentList(
                                    SingletonSeparatedList<TypeSyntax>(
                                        IdentifierName(csElement.PublicType.QualifiedName))))),
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    publicElement))))));
        }
    }
}
