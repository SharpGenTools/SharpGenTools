using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace SharpGen.Generator
{
    class ParameterPrologCodeGenerator : ParameterPrologEpilogBase, IMultiCodeGenerator<CsParameter, StatementSyntax>
    {
        public ParameterPrologCodeGenerator(GlobalNamespaceProvider globalNamespace)
        {
            this.globalNamespace = globalNamespace;
        }

        GlobalNamespaceProvider globalNamespace;

        public IEnumerable<StatementSyntax> GenerateCode(CsParameter csElement)
        {
            // predeclare return type parameter
            if (csElement.IsUsedAsReturnType)
            {
                yield return LocalDeclarationStatement(
                    VariableDeclaration(
                        csElement.IsArray ? ArrayType(ParseTypeName(csElement.PublicType.QualifiedName), SingletonList(ArrayRankSpecifier())) : ParseTypeName(csElement.PublicType.QualifiedName),
                        SingletonSeparatedList(
                            VariableDeclarator(csElement.Name))));
            }
            // In-Optional parameters
            if (csElement.IsArray && csElement.IsValueType && !csElement.HasNativeValueType)
            {
                if (csElement.IsOptional)
                {
                    yield return LocalDeclarationStatement(
                        VariableDeclaration(
                            ArrayType(ParseTypeName(csElement.PublicType.QualifiedName), SingletonList(ArrayRankSpecifier())),
                            SingletonSeparatedList(
                                VariableDeclarator($"{csElement.TempName}_")
                                    .WithInitializer(EqualsValueClause(IdentifierName(csElement.Name))))));
                }
                yield break;
            }
            // handle native marshalling if needed
            if (csElement.HasNativeValueType)
            {
                if (csElement.IsArray)
                {
                    yield return LocalDeclarationStatement(
                        VariableDeclaration(
                            ArrayType(ParseTypeName($"{csElement.PublicType.QualifiedName}.__Native"), SingletonList(ArrayRankSpecifier())),
                            SingletonSeparatedList(
                                VariableDeclarator($"{csElement.TempName}_")
                                    .WithInitializer(EqualsValueClause(
                                        GenerateNullCheckIfNeeded(csElement, false,
                                            ObjectCreationExpression(
                                                ArrayType(ParseTypeName($"{csElement.PublicType.QualifiedName}.__Native"),
                                                SingletonList(ArrayRankSpecifier(
                                                    SingletonSeparatedList<ExpressionSyntax>(
                                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                            IdentifierName(csElement.Name),
                                                            IdentifierName("Length"))))))),
                                            LiteralExpression(SyntaxKind.NullLiteralExpression)))))));
                    if (csElement.IsRefIn)
                    {
                        yield return LoopThroughArrayParameter(csElement.Name,
                            CreateMarshalStructStatement(
                                csElement,
                                "__MarshalTo",
                                ElementAccessExpression(
                                    IdentifierName(csElement.Name),
                                    BracketedArgumentList(
                                        SingletonSeparatedList(
                                            Argument(IdentifierName("i"))))),
                                ElementAccessExpression(
                                    IdentifierName($"{csElement.TempName}_"),
                                    BracketedArgumentList(
                                        SingletonSeparatedList(
                                            Argument(IdentifierName("i")))))),
                            "i"); 

                    }
                }
                else
                {
                    yield return LocalDeclarationStatement(
                        VariableDeclaration(IdentifierName("var"),
                            SingletonSeparatedList(
                                VariableDeclarator(csElement.TempName)
                                    .WithInitializer(
                                        EqualsValueClause(
                                            ParseExpression(((CsStruct)csElement.PublicType).GetConstructor()))))));

                    ExpressionSyntax publicElementExpression = IdentifierName(csElement.Name);

                    if (csElement.IsNullableStruct)
                    {
                        publicElementExpression = MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            publicElementExpression,
                            IdentifierName("Value"));
                    }
                    
                    if (csElement.IsRefIn || csElement.IsRef || csElement.IsIn)
                    {
                        yield return GenerateNullCheckIfNeeded(csElement, true,
                            CreateMarshalStructStatement(
                                csElement,
                                "__MarshalTo",
                                publicElementExpression,
                                IdentifierName(csElement.TempName)));
                    }
                }
            }
            // handle out parameters
            else if (csElement.IsOut)
            {
                if (csElement.IsValueType && !csElement.IsPrimitive)
                {
                    yield return ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(csElement.Name),
                            ObjectCreationExpression(ParseTypeName(csElement.PublicType.QualifiedName))
                                .WithArgumentList(ArgumentList())
                    ));
                }
                else if (csElement.IsBoolToInt && !csElement.IsArray)
                {
                    yield return LocalDeclarationStatement(
                        VariableDeclaration(PredefinedType(Token(SyntaxKind.IntKeyword)),
                            SingletonSeparatedList(VariableDeclarator(csElement.TempName))));
                }
                else if (csElement.IsInterface)
                {
                    if (csElement.IsArray)
                    {
                        yield return LocalDeclarationStatement(
                            VariableDeclaration(
                                PointerType(
                                    QualifiedName(
                                        IdentifierName("System"),
                                        IdentifierName("IntPtr"))),
                                SingletonSeparatedList(
                                    VariableDeclarator(csElement.TempName)
                                        .WithInitializer(
                                            EqualsValueClause(
                                                StackAllocArrayCreationExpression(
                                                    ArrayType(
                                                        QualifiedName(
                                                            IdentifierName("System"),
                                                            IdentifierName("IntPtr")),
                                                        SingletonList(
                                                            ArrayRankSpecifier(
                                                                SingletonSeparatedList(
                                                                    GenerateNullCheckIfNeeded(csElement, true,
                                                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                            IdentifierName(csElement.Name),
                                                                            IdentifierName("Length")),
                                                                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))
                                )))))))))));
                    }
                    else
                    {
                        yield return LocalDeclarationStatement(
                            VariableDeclaration(
                                QualifiedName(
                                    IdentifierName("System"),
                                    IdentifierName("IntPtr")),
                                SingletonSeparatedList(
                                    VariableDeclarator(csElement.TempName)
                                        .WithInitializer(
                                            EqualsValueClause(
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("System"),
                                                        IdentifierName("IntPtr")),
                                                    IdentifierName("Zero")))))));
                    }
                }
            }
            // handle array [In] parameters
            else if (csElement.IsArray)
            {
                if (csElement.IsInterface)
                {
                    yield return LocalDeclarationStatement(
                        VariableDeclaration(
                            PointerType(
                                QualifiedName(
                                    IdentifierName("System"),
                                    IdentifierName("IntPtr"))),
                            SingletonSeparatedList(
                                VariableDeclarator(csElement.TempName))));
                    yield return ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(csElement.TempName),
                            CastExpression(
                                PointerType(
                                    QualifiedName(
                                        IdentifierName("System"),
                                        IdentifierName("IntPtr"))),
                                LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    Literal(0)))));
                    yield return GenerateNullCheckIfNeeded(csElement, false,
                        Block(
                        new StatementSyntax[] {
                                LocalDeclarationStatement(
                                    VariableDeclaration(
                                        PointerType(
                                            QualifiedName(
                                                IdentifierName("System"),
                                                IdentifierName("IntPtr"))),
                                        SingletonSeparatedList(
                                            VariableDeclarator(
                                                Identifier($"{csElement.TempName}_"))
                                            .WithInitializer(
                                                EqualsValueClause(
                                                    StackAllocArrayCreationExpression(
                                                        ArrayType(
                                                            QualifiedName(
                                                                IdentifierName("System"),
                                                                IdentifierName("IntPtr")),
                                                            SingletonList(
                                                                    ArrayRankSpecifier(
                                                                        SingletonSeparatedList<ExpressionSyntax>(
                                                                            MemberAccessExpression(
                                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                                IdentifierName(csElement.Name),
                                                                                IdentifierName("Length")))))))))))),
                                ExpressionStatement(
                                    AssignmentExpression(
                                        SyntaxKind.SimpleAssignmentExpression,
                                        IdentifierName(csElement.TempName),
                                        IdentifierName($"{csElement.TempName}_"))),
                                LoopThroughArrayParameter(csElement.Name,
                                    ExpressionStatement(
                                        AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            ElementAccessExpression(
                                                IdentifierName(csElement.TempName),
                                                BracketedArgumentList(
                                                    SingletonSeparatedList(
                                                        Argument(
                                                            IdentifierName("i"))))),
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
                                                            ElementAccessExpression(
                                                                IdentifierName(csElement.Name),
                                                                BracketedArgumentList(
                                                                    SingletonSeparatedList(
                                                                        Argument(
                                                                            IdentifierName("i")))))))))
                                        )),
                                    "i")
                        }));
                }
            }
            // handle string parameters
            else if (csElement.IsString && !csElement.IsWideChar)
            {
                yield return LocalDeclarationStatement(
                    VariableDeclaration(
                        QualifiedName(
                            IdentifierName("System"),
                            IdentifierName("IntPtr")),
                        SingletonSeparatedList(
                            VariableDeclarator(csElement.TempName)
                                .WithInitializer(
                                    EqualsValueClause(
                                        InvocationExpression(
                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                globalNamespace.GetTypeNameSyntax(BuiltinType.Marshal),
                                                IdentifierName("StringToHGlobalAnsi")),
                                            ArgumentList(
                                                SingletonSeparatedList(
                                                    Argument(IdentifierName(csElement.Name))))))))));
            }
            else if (csElement.IsRefIn && csElement.IsValueType && csElement.IsOptional)
            {
                yield return LocalDeclarationStatement(
                    VariableDeclaration(ParseTypeName(csElement.PublicType.QualifiedName),
                        SingletonSeparatedList(
                            VariableDeclarator(csElement.TempName))));
                yield return GenerateNullCheckIfNeeded(csElement, false,
                    ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(csElement.TempName),
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(csElement.Name),
                                IdentifierName("Value")))));
            }
        }

    }
}
