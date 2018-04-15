using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;

namespace SharpGen.Generator
{
    /// <summary>
    /// Generates code to marshal data for a parameter/field/return value from its native representation.
    /// </summary>
    class MarshalFromNativeCodeGenerator : MarshallingCodeGeneratorBase, ICodeGenerator<CsMarshalBase, StatementSyntax>
    {
        private readonly GlobalNamespaceProvider globalNamespace;
        private readonly bool singleStackFrame;
        private bool MarshalPinnableElements => !singleStackFrame;

        public MarshalFromNativeCodeGenerator(bool singleStack, GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
            this.singleStackFrame = singleStack;
            this.globalNamespace = globalNamespace;
        }

        public StatementSyntax GenerateCode(CsMarshalBase csElement)
        {
            if (csElement.IsArray)
            {
                if (csElement.HasNativeValueType)
                {
                    return LoopThroughArrayParameter(
                        csElement,
                        (publicElement, marshalElement) =>
                            CreateMarshalStructStatement(
                                csElement,
                                "__MarshalFrom",
                                publicElement,
                                marshalElement));
                }
                else if (csElement.IsInterface)
                {
                    return LoopThroughArrayParameter(
                        csElement,
                        (publicElement, marshalElement) =>
                            MarshalInterfaceInstanceFromNative(csElement, publicElement, marshalElement));
                }
                else if (csElement.IsBoolToInt)
                {
                    if (singleStackFrame)
                    {
                        return GenerateNullCheckIfNeeded(csElement,
                            ExpressionStatement(
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            globalNamespace.GetTypeNameSyntax(WellKnownName.BooleanHelpers),
                                            IdentifierName("ConvertToBoolArray")))
                                    .WithArgumentList(
                                        ArgumentList(
                                            SeparatedList(
                                                new[]
                                                {
                                                    Argument(GetMarshalStorageLocation(csElement)),
                                                    Argument(IdentifierName(csElement.Name))
                                                }
                                    )))));
                    }
                    else
                    {
                        return GenerateNullCheckIfNeeded(csElement,
                            FixedStatement(
                                VariableDeclaration(
                                    PointerType(
                                        ParseTypeName(csElement.MarshalType.QualifiedName)))
                                .WithVariables(
                                    SingletonSeparatedList(
                                        VariableDeclarator(
                                            Identifier("__ptr"))
                                        .WithInitializer(
                                            EqualsValueClause(
                                                PrefixUnaryExpression(
                                                    SyntaxKind.AddressOfExpression,
                                                   GetMarshalStorageLocation(csElement)))))),
                                ExpressionStatement(
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            globalNamespace.GetTypeNameSyntax(WellKnownName.BooleanHelpers),
                                            IdentifierName("ConvertToBoolArray")))
                                    .WithArgumentList(
                                        ArgumentList(
                                            SeparatedList(
                                                new[]
                                                {
                                                    Argument(IdentifierName("__ptr")),
                                                    Argument(IdentifierName(csElement.Name))
                                                }
                                    ))))));
                    }
                }
                else if (csElement.IsString) // Character array presented to the user as a string.
                {
                    if (!csElement.IsWideChar || MarshalPinnableElements)
                    {
                        return FixedStatement(
                            VariableDeclaration(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                                SingletonSeparatedList(
                                    VariableDeclarator("__ptr")
                                    .WithInitializer(EqualsValueClause(
                                        PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                            GetMarshalStorageLocation(csElement))
                                    )))),
                            ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(csElement.Name),
                            InvocationExpression(
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    globalNamespace.GetTypeNameSyntax(WellKnownName.StringHelpers),
                                    IdentifierName("PtrToString" + (csElement.IsWideChar ? "Uni" : "Ansi"))),
                                ArgumentList(SeparatedList(
                                    new[]
                                    {
                                        Argument(CastExpression(ParseTypeName("System.IntPtr"), IdentifierName("__ptr"))),
                                        Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(csElement.ArrayDimensionValue - 1)))
                                    }
                                    ))))));
                    }
                    return null;
                }
                else if (csElement.IsValueType)
                {
                    if (MarshalPinnableElements)
                    {
                        return GenerateCopyMemory(csElement, copyFromNative: true); 
                    }

                    return null;
                }
                throw new ArgumentException($"Missing array marshalling from native for {csElement}.", nameof(csElement));
            }
            else if (csElement.IsString)
            {
                if (!csElement.IsWideChar || MarshalPinnableElements)
                {
                    return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                    IdentifierName(csElement.Name),
                                    InvocationExpression(
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            globalNamespace.GetTypeNameSyntax(BuiltinType.Marshal),
                                            IdentifierName("PtrToString" + (csElement.IsWideChar ? "Uni" : "Ansi"))),
                                        ArgumentList(SingletonSeparatedList(
                                            Argument(
                                                GetMarshalStorageLocation(csElement)))))));
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
                    "__MarshalFrom",
                    publicElementExpression,
                    GetMarshalStorageLocation(csElement));
            }
            else if (csElement.IsInterface)
            {
                return MarshalInterfaceInstanceFromNative(
                    csElement,
                    IdentifierName(csElement.Name),
                    GetMarshalStorageLocation(csElement));
            }
            else if (csElement.MappedToDifferentPublicType)
            {
                return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(csElement.Name),
                        CastExpression(ParseTypeName(csElement.PublicType.QualifiedName),
                            GetMarshalStorageLocation(csElement))));
            }
            else if (csElement.IsBoolToInt && !(csElement is CsField))
            {
                return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(csElement.Name),
                        BinaryExpression(
                            SyntaxKind.NotEqualsExpression,
                            GetMarshalStorageLocation(csElement),
                            LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                Literal(0)))));
            }
            else if (csElement is CsField field) // Fields are always marshalled
            {
                if (csElement.IsBoolToInt) // integer value cached in intermediate field.
                {
                    return ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(csElement.IntermediateMarshalName),
                        GetMarshalStorageLocation(csElement)));
                }
                else if (field.IsBitField)
                {
                    return ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(csElement.IntermediateMarshalName),
                        GetMarshalStorageLocation(csElement)));
                }
                return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(csElement.Name),
                        GetMarshalStorageLocation(csElement)));
            }
            return null;
        }

        private StatementSyntax MarshalInterfaceInstanceFromNative(CsMarshalBase csElement, ExpressionSyntax publicElement, ExpressionSyntax marshalElement)
        {
            var interfaceType = (CsInterface)csElement.PublicType;

            if (csElement.IsFastOut)
            {
                return ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                ParenthesizedExpression(publicElement),
                                IdentifierName("NativePointer")),
                            marshalElement));
            }
            
            return IfStatement(
                    BinaryExpression(SyntaxKind.NotEqualsExpression,
                        marshalElement,
                        ParseExpression("System.IntPtr.Zero")),
                    ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        publicElement,
                        ObjectCreationExpression(ParseTypeName(interfaceType.GetNativeImplementationOrThis().QualifiedName))
                        .WithArgumentList(
                            ArgumentList(SingletonSeparatedList(
                                Argument(marshalElement)))))),
                    ElseClause(
                        ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            publicElement,
                            LiteralExpression(SyntaxKind.NullLiteralExpression,
                                Token(SyntaxKind.NullKeyword))))));
        }
    }
}
