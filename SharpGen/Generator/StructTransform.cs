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

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SharpGen.Logging;
using SharpGen.CppModel;
using SharpGen.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.CSharp;
using System;

namespace SharpGen.Generator
{
    /// <summary>
    /// Transforms a C++ struct to a C# struct.
    /// </summary>
    public class StructTransform : TransformBase<CsStruct, CppStruct>
    {
        private readonly Dictionary<Regex, string> _mapMoveStructToInner = new Dictionary<Regex, string>();

        public override SyntaxNode GenerateCode(CsStruct csElement)
        {
            var documentationTrivia = GenerateDocumentationTrivia(csElement);
            var layoutKind = csElement.ExplicitLayout ? "Explicit" : "Sequential";
            var structLayoutAttribute = Attribute(ParseName("System.Runtime.InteropServices.StructLayoutAttribute"))
                    .WithArgumentList(
                        AttributeArgumentList(SeparatedList<AttributeArgumentSyntax>(
                            new SyntaxNodeOrToken[]
                            {
                            AttributeArgument(ParseName($"System.Runtime.InteropServices.LayoutKind.{layoutKind}")),
                            AttributeArgument(
                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(csElement.Align)))
                                .WithNameEquals(NameEquals(IdentifierName("Pack")))
                            }
                        )
                    )
                );

            var innerStructs = csElement.InnerStructs.Select(GenerateCode).Cast<MemberDeclarationSyntax>();

            var constants = csElement.Variables.Select(var => GenerateConstant(var));

            var fields = csElement.Fields.SelectMany(field => GenerateFieldAccessors(field, !csElement.HasMarshalType && csElement.ExplicitLayout));

            var marshallingStructAndConversions = GenerateMarshallingStructAndConversions(csElement, AttributeList(SingletonSeparatedList(structLayoutAttribute)));

            return (csElement.GenerateAsClass ?
                (MemberDeclarationSyntax)ClassDeclaration(
                    SingletonList(AttributeList(csElement.HasMarshalType ? SingletonSeparatedList(structLayoutAttribute) : default)),
                    TokenList(ParseTokens(csElement.VisibilityName)),
                    Identifier(csElement.Name),
                    TypeParameterList(SeparatedList(Enumerable.Empty<TypeParameterSyntax>())),
                    BaseList(SeparatedList(Enumerable.Empty<BaseTypeSyntax>())),
                    List(Enumerable.Empty<TypeParameterConstraintClauseSyntax>()),
                    List(innerStructs.Concat(constants).Concat(fields).Concat(marshallingStructAndConversions)))
                .WithLeadingTrivia(Trivia(documentationTrivia))
                    :
                StructDeclaration(
                    SingletonList(AttributeList(csElement.HasMarshalType ? SingletonSeparatedList(structLayoutAttribute) : default)),
                    TokenList(ParseTokens(csElement.VisibilityName)),
                    Identifier(csElement.Name),
                    TypeParameterList(SeparatedList(Enumerable.Empty<TypeParameterSyntax>())),
                    BaseList(SeparatedList(Enumerable.Empty<BaseTypeSyntax>())),
                    List(Enumerable.Empty<TypeParameterConstraintClauseSyntax>()),
                    List(innerStructs.Concat(constants).Concat(fields).Concat(marshallingStructAndConversions))))
                .WithLeadingTrivia(Trivia(documentationTrivia));
        }

        private IEnumerable<MemberDeclarationSyntax> GenerateFieldAccessors(CsField field, bool explicitLayout)
        {
            var docComments = GenerateDocumentationTrivia(field);
            if (field.IsBoolToInt && !field.IsArray)
            {
                yield return PropertyDeclaration(PredefinedType(Token(SyntaxKind.BoolKeyword)), field.Name)
                    .WithAccessorList(
                    AccessorList(
                        List(
                            new[]
                            {
                                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                    .WithExpressionBody(ArrowExpressionClause(
                                        BinaryExpression(SyntaxKind.NotEqualsExpression,
                                                ParseName($"_{field.Name}"),
                                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))))),
                                AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                    .WithExpressionBody(ArrowExpressionClause(CastExpression(ParseTypeName(field.PublicType.QualifiedName), ParseName("value"))))
                            })))
                    .WithModifiers(TokenList(Token(TriviaList(Trivia(docComments)), ParseToken(field.VisibilityName).Kind(), TriviaList())));
                yield return GenerateBackingField(field, explicitLayout, null);
            }
            else if (field.IsArray && field.QualifiedName != "System.String")
            {
                yield return PropertyDeclaration(ArrayType(ParseTypeName(field.PublicType.QualifiedName)), field.Name)
                    .WithAccessorList(
                        AccessorList(
                            List(
                                new[]
                                {
                                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                        .WithExpressionBody(ArrowExpressionClause(
                                            BinaryExpression(SyntaxKind.CoalesceExpression,
                                            ParseName($"_{field.Name}"),
                                            AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                ParseName($"_{field.Name}"),
                                                ObjectCreationExpression(
                                                    ArrayType(ParseTypeName(field.PublicType.QualifiedName),
                                                    List(new [] {
                                                        ArrayRankSpecifier(
                                                            SingletonSeparatedList<ExpressionSyntax>(
                                                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(field.ArrayDimensionValue))))
                                                    })))))))
                                })))
                    .WithModifiers(TokenList(Token(TriviaList(Trivia(docComments)), ParseToken(field.VisibilityName).Kind(), TriviaList())));
                yield return GenerateBackingField(field, explicitLayout, null, true);
            }
            else if(field.IsBitField)
            {
                if (field.BitMask == 1)
                {
                    yield return PropertyDeclaration(PredefinedType(Token(SyntaxKind.BoolKeyword)), field.Name)
                        .WithAccessorList(
                            AccessorList(
                                SingletonList(
                                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                        .WithExpressionBody(ArrowExpressionClause(
                                            BinaryExpression(SyntaxKind.NotEqualsExpression,
                                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)),
                                                BinaryExpression(SyntaxKind.BitwiseAndExpression,
                                                    BinaryExpression(SyntaxKind.RightShiftExpression,
                                                        ParseName(field.Name),
                                                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(field.BitOffset))),
                                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(field.BitMask))))))
                                    )))
                    .WithModifiers(TokenList(Token(TriviaList(Trivia(docComments)), ParseToken(field.VisibilityName).Kind(), TriviaList())));
                }
                else
                {

                    yield return PropertyDeclaration(ParseTypeName(field.PublicType.QualifiedName), field.Name)
                        .WithAccessorList(
                            AccessorList(
                                SingletonList(
                                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                        .WithExpressionBody(ArrowExpressionClause(
                                            CastExpression(ParseTypeName(field.PublicType.QualifiedName),
                                                BinaryExpression(SyntaxKind.BitwiseAndExpression,
                                                    BinaryExpression(SyntaxKind.RightShiftExpression,
                                                        ParseName(field.Name),
                                                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(field.BitOffset))),
                                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(field.BitMask))))))
                                    )))
                    .WithModifiers(TokenList(Token(TriviaList(Trivia(docComments)), ParseToken(field.VisibilityName).Kind(), TriviaList())));
                }
                yield return GenerateBackingField(field, explicitLayout, null);
            }
            else
            {
                yield return GenerateBackingField(field, explicitLayout, docComments);
            }
        }

        private static MemberDeclarationSyntax GenerateBackingField(CsField field, bool explicitLayout, DocumentationCommentTriviaSyntax docTrivia, bool isArray = false)
        {
            var fieldDecl = FieldDeclaration(
                VariableDeclaration(isArray ?
                    ArrayType(ParseTypeName(field.PublicType.QualifiedName))
                    : ParseTypeName(field.PublicType.QualifiedName),
                    SingletonSeparatedList(
                        VariableDeclarator(field.Name)
                    )));
            var visibilityToken = docTrivia != null ?
                Token(TriviaList(Trivia(docTrivia)), ParseToken(field.VisibilityName).Kind(), TriviaList())
                : Token(SyntaxKind.InternalKeyword);

            fieldDecl = fieldDecl.WithModifiers(TokenList(visibilityToken));

            if (explicitLayout)
            {
                fieldDecl = fieldDecl.WithAttributeLists(SingletonList(
                    AttributeList(
                        SingletonSeparatedList(Attribute(
                            ParseName("System.Runtime.InteropServices.FieldOffset"),
                            AttributeArgumentList(
                                SingletonSeparatedList(AttributeArgument(
                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(field.Offset))))))))
                ));
            }
            return fieldDecl;
        }

        private static FieldDeclarationSyntax GenerateConstant(CsVariable var)
        {
            return FieldDeclaration(
                        VariableDeclaration(
                            IdentifierName("TypeName"))
                        .WithVariables(
                            SingletonSeparatedList(
                                VariableDeclarator(
                                    Identifier("name"))
                                .WithInitializer(
                                    EqualsValueClause(
                                        LiteralExpression(
                                            SyntaxKind.NumericLiteralExpression,
                                            Literal(0)))))))
                    .WithModifiers(
                        TokenList(
                            Token(GenerateConstantDocumentationTrivia(var),
                                ParseToken(var.VisibilityName).Kind(),
                                TriviaList())));


            SyntaxTriviaList GenerateConstantDocumentationTrivia(CsVariable csVar)
            {
                return TriviaList(
                    Trivia(
                        DocumentationCommentTrivia(
                            SyntaxKind.SingleLineDocumentationCommentTrivia,
                            List(
                                new XmlNodeSyntax[]{
                                XmlText()
                                .WithTextTokens(
                                    TokenList(
                                        XmlTextLiteral(
                                            TriviaList(
                                                DocumentationCommentExterior("///")),
                                            " ",
                                            " ",
                                            TriviaList()))),
                                XmlExampleElement(
                                    SingletonList<XmlNodeSyntax>(
                                        XmlText()
                                        .WithTextTokens(
                                            TokenList(
                                                XmlTextLiteral(
                                                    TriviaList(),
                                                    $"Constant {csVar.Name}.",
                                                    $"Constant {csVar.Name}.",
                                                    TriviaList())))))
                                .WithStartTag(
                                    XmlElementStartTag(
                                        XmlName(
                                            Identifier("summary"))))
                                .WithEndTag(
                                    XmlElementEndTag(
                                        XmlName(
                                            Identifier("summary")))),
                                XmlText()
                                .WithTextTokens(
                                    TokenList(
                                        new []{
                                            XmlTextNewLine(
                                                TriviaList(),
                                                "\n",
                                                "\n",
                                                TriviaList()),
                                            XmlTextLiteral(
                                                TriviaList(
                                                    DocumentationCommentExterior("///")),
                                                " ",
                                                " ",
                                                TriviaList())})),
                                XmlExampleElement(
                                    SingletonList<XmlNodeSyntax>(
                                        XmlText()
                                        .WithTextTokens(
                                            TokenList(
                                                XmlTextLiteral(
                                                    TriviaList(),
                                                    csVar.CppElementName,
                                                    csVar.CppElementName,
                                                    TriviaList())))))
                                .WithStartTag(
                                    XmlElementStartTag(
                                        XmlName(
                                            Identifier("unmanaged"))))
                                .WithEndTag(
                                    XmlElementEndTag(
                                        XmlName(
                                            Identifier("unmanaged")))),
                                XmlText()
                                .WithTextTokens(
                                    TokenList(
                                        XmlTextNewLine(
                                            TriviaList(),
                                            "\n",
                                            "\n",
                                            TriviaList())))}))));
            }

        }

        private IEnumerable<MemberDeclarationSyntax> GenerateMarshallingStructAndConversions(CsStruct csStruct, AttributeListSyntax structLayoutAttributeList)
        {
            if (!csStruct.HasMarshalType || csStruct.HasCustomMarshal)
            {
                yield break;
            }

            var marshalStruct = StructDeclaration("__Native")
                .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.PartialKeyword)))
                .WithAttributeLists(SingletonList(structLayoutAttributeList))
                .WithMembers(List(csStruct.Fields.SelectMany(GenerateMarshalStructField)))
                .AddMembers(GenerateNativeMarshalFree());
            yield return marshalStruct;

            yield return MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), "__MarshalFree")
                .WithParameterList(ParameterList(SingletonSeparatedList(
                    Parameter(Identifier("@ref")).WithType(RefType(ParseTypeName("__Native"))))))
                .WithExpressionBody(ArrowExpressionClause(InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("@ref"), IdentifierName("__MarshalFree")))))
                .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword)));

            yield return GenerateMarshalFrom();

            if (csStruct.IsOut)
            {
                yield return GenerateMarshalTo();
            }
            
            IEnumerable<MemberDeclarationSyntax> GenerateMarshalStructField(CsField field)
            {
                var fieldDecl = FieldDeclaration(
                        VariableDeclaration(
                            ParseTypeName(field.MarshalType.QualifiedName)))
                            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)));
                if (csStruct.ExplicitLayout)
                {
                    fieldDecl = fieldDecl.WithAttributeLists(SingletonList(
                        AttributeList(
                            SingletonSeparatedList(Attribute(
                                ParseName("System.Runtime.InteropServices.FieldOffset"),
                                AttributeArgumentList(
                                    SingletonSeparatedList(AttributeArgument(
                                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(field.Offset))))))))
                    ));
                }
                if (field.IsArray)
                {
                    yield return fieldDecl.WithDeclaration(fieldDecl.Declaration.AddVariables(
                        VariableDeclarator(field.Name)
                        ));
                    for (int i = 1; i < field.ArrayDimensionValue; i++)
                    {
                        var declaration = fieldDecl.WithDeclaration(fieldDecl.Declaration.AddVariables(VariableDeclarator($"__{field.Name}{i}")));
                        if (csStruct.ExplicitLayout)
                        {
                            var offset = field.Offset + (field.SizeOf / field.ArrayDimensionValue) * i;
                            declaration = declaration.WithAttributeLists(SingletonList(
                                AttributeList(
                                    SingletonSeparatedList(Attribute(
                                        ParseName("System.Runtime.InteropServices.FieldOffset"),
                                        AttributeArgumentList(
                                            SingletonSeparatedList(AttributeArgument(
                                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(offset))))))))
                            ));
                        }
                        yield return declaration;
                    }

                }
                else if (field.IsBoolToInt)
                {
                    yield return fieldDecl.WithDeclaration(fieldDecl.Declaration.AddVariables(
                        VariableDeclarator($"_{field.Name}")
                        ));
                }
                else if (field.PublicType is CsStruct fieldType && fieldType.HasMarshalType)
                {
                    yield return fieldDecl.WithDeclaration(VariableDeclaration(
                        ParseTypeName($"{field.MarshalType.QualifiedName}.__Native"), SingletonSeparatedList(
                            VariableDeclarator(field.Name))));
                }
                else
                {
                    yield return fieldDecl.WithDeclaration(fieldDecl.Declaration.AddVariables(VariableDeclarator(field.Name)));
                }
            }

            MethodDeclarationSyntax GenerateNativeMarshalFree()
                => MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), "__MarshalFree").WithBody(
                    Block(
                        List(csStruct.Fields.Where(field => !field.IsArray).Select(field =>
                        {
                            if (field.PublicType.QualifiedName == "System.String")
                            {
                                return IfStatement(
                                    BinaryExpression(SyntaxKind.NotEqualsExpression,
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            ThisExpression(),
                                            IdentifierName(field.Name)),
                                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))),
                                    ExpressionStatement(InvocationExpression(
                                        ParseExpression("System.Runtime.InteropServices.Marshal.FreeHGlobal"),
                                        ArgumentList(SingletonSeparatedList(
                                            Argument(
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                ThisExpression(),
                                                IdentifierName(field.Name))))))));
                            }
                            else if (field.PublicType is CsStruct fieldStruct && fieldStruct.HasMarshalType)
                            {
                                return ExpressionStatement(
                                InvocationExpression(
                                 MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                     MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                         ThisExpression(),
                                         IdentifierName(field.Name)),
                                     IdentifierName("__MarshalFree"))));
                            }
                            else
                            {
                                return (StatementSyntax)EmptyStatement();
                            }
                        }
                        ))))
                .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.UnsafeKeyword)));

            MethodDeclarationSyntax GenerateMarshalFrom()
            {
                return MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), "__MarshalFrom")
                    .WithParameterList(ParameterList(SingletonSeparatedList(
                        Parameter(Identifier("@ref")).WithType(RefType(ParseTypeName("__Native"))))))
                    .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword)))
                    .WithBody(Block(
                        csStruct.Fields.Select(field =>
                        {
                            if (field.IsBoolToInt)
                            {
                                if (field.IsArray)
                                {
                                    return FixedStatement(
                                        VariableDeclaration(
                                            PointerType(PredefinedType(Token(SyntaxKind.IntKeyword))),
                                            SingletonSeparatedList(
                                                VariableDeclarator("__from")
                                                    .WithInitializer(EqualsValueClause(PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("@ref"),
                                                        IdentifierName(field.Name))))))),
                                        ExpressionStatement(
                                            AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    ThisExpression(),
                                                    IdentifierName($"_{field.Name}")),
                                                InvocationExpression(
                                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                            IdentifierName(Manager.GlobalNamespace.Name),
                                                            IdentifierName("Utilities")),
                                                        IdentifierName("ConvertToBoolArray")),
                                                    ArgumentList(SeparatedList(new []
                                                    {
                                                        Argument(IdentifierName("__from")),
                                                        Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(field.ArrayDimensionValue)))
                                                    })))
                                            )));
                                }
                                else
                                {
                                    return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            ThisExpression(),
                                            IdentifierName($"_{field.Name}")),
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("@ref"),
                                            IdentifierName($"_{field.Name}"))));
                                }
                            }
                            else if (field.IsArray)
                            {
                                if (field.IsFixedArrayOfStruct)
                                {
                                    return GenerateArrayCopyMemory(field, copyFromNative: true);
                                }
                                else
                                {
                                    if (field.MarshalType.QualifiedName == "System.Char")
                                    {
                                        return FixedStatement(
                                            VariableDeclaration(
                                                PointerType(PredefinedType(Token(SyntaxKind.CharKeyword))),
                                                SingletonSeparatedList(
                                                    VariableDeclarator("__ptr")
                                                        .WithInitializer(EqualsValueClause(PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("@ref"), IdentifierName(field.Name))))))),
                                            ExpressionStatement(
                                                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        ThisExpression(),
                                                        IdentifierName(field.Name)),
                                                    InvocationExpression(
                                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                IdentifierName(Manager.GlobalNamespace.Name),
                                                                IdentifierName("Utilities")),
                                                            IdentifierName("PtrToStringUni")),
                                                        ArgumentList(SeparatedList(new[]
                                                        {
                                                            Argument(
                                                                CastExpression(ParseTypeName("System.IntPtr"),
                                                                IdentifierName("__ptr"))),
                                                            Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(field.ArrayDimensionValue)))
                                                        })))
                                                )));
                                    }
                                    else if (field.PublicType.QualifiedName == "System.String" && field.MarshalType.QualifiedName == "System.Byte")
                                    {
                                        return FixedStatement(
                                            VariableDeclaration(
                                                PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                                                SingletonSeparatedList(
                                                    VariableDeclarator("__ptr")
                                                        .WithInitializer(EqualsValueClause(PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("@ref"), IdentifierName(field.Name))))))),
                                            ExpressionStatement(
                                                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        ThisExpression(),
                                                        IdentifierName(field.Name)),
                                                    InvocationExpression(
                                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                IdentifierName(Manager.GlobalNamespace.Name),
                                                                IdentifierName("Utilities")),
                                                            IdentifierName("PtrToStringAnsi")),
                                                        ArgumentList(SeparatedList(new[]
                                                        {
                                                            Argument(
                                                                CastExpression(ParseTypeName("System.IntPtr"),
                                                                IdentifierName("__ptr"))),
                                                            Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(field.ArrayDimensionValue)))
                                                        })))
                                                )));
                                    }
                                    else
                                    {
                                        return GenerateArrayCopyMemory(field, copyFromNative: true);
                                    }
                                }
                            }
                            else
                            {
                                if (field.PublicType.QualifiedName == "System.String")
                                {
                                    return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            ThisExpression(),
                                            IdentifierName(field.Name)),
                                        ConditionalExpression(
                                                BinaryExpression(SyntaxKind.EqualsExpression,
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName("@ref"),
                                                    IdentifierName(field.Name)),
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("System"),
                                                        IdentifierName("IntPtr")),
                                                    IdentifierName("Zero"))),
                                                LiteralExpression(SyntaxKind.NullLiteralExpression),
                                                InvocationExpression(
                                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                        ParseTypeName("System.Runtime.InteropServices.Marshal"),
                                                        IdentifierName("PtrToString" + (field.IsWideChar ? "Uni" : "Ansi"))),
                                                    ArgumentList(SingletonSeparatedList(
                                                        Argument(
                                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                IdentifierName("@ref"),
                                                                IdentifierName(field.Name)))))))));
                                }
                                else if (field.PublicType is CsStruct structType && structType.HasMarshalType)
                                {
                                    return Block(
                                            ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    ThisExpression(),
                                                    IdentifierName(field.Name)),
                                                ObjectCreationExpression(ParseTypeName(field.PublicType.QualifiedName))
                                            )),
                                            ExpressionStatement(InvocationExpression(
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                        ThisExpression(),
                                                        IdentifierName(field.Name)),
                                                    IdentifierName("__MarshalFrom")),
                                                ArgumentList(SingletonSeparatedList(
                                                        Argument(
                                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                IdentifierName("@ref"),
                                                                IdentifierName(field.Name)))
                                                            .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)))))));
                                }
                                else
                                {
                                    return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            ThisExpression(),
                                            IdentifierName(field.Name)),
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("@ref"),
                                            IdentifierName(field.Name))));
                                }
                            }
                        }
                        )));
            }

            MethodDeclarationSyntax GenerateMarshalTo()
            {
                return MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), "__MarshalTo")
                    .WithParameterList(ParameterList(SingletonSeparatedList(
                        Parameter(Identifier("@ref")).WithType(RefType(ParseTypeName("__Native"))))))
                    .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword)))
                    .WithBody(Block(
                        csStruct.Fields.Select(field =>
                        {
                            if (field.IsBoolToInt)
                            {
                                if (field.IsArray)
                                {
                                    return FixedStatement(
                                        VariableDeclaration(
                                            PointerType(PredefinedType(Token(SyntaxKind.IntKeyword))),
                                            SingletonSeparatedList(
                                                VariableDeclarator("__from")
                                                    .WithInitializer(EqualsValueClause(PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("@ref"),
                                                        IdentifierName(field.Name))))))),
                                        ExpressionStatement(
                                            AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    ThisExpression(),
                                                    IdentifierName($"_{field.Name}")),
                                                InvocationExpression(
                                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                            IdentifierName(Manager.GlobalNamespace.Name),
                                                            IdentifierName("Utilities")),
                                                        IdentifierName("ConvertToIntArray")),
                                                    ArgumentList(SeparatedList(new[]
                                                    {
                                                        Argument(IdentifierName("__from")),
                                                        Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(field.ArrayDimensionValue)))
                                                    })))
                                            )));
                                }
                                else
                                {
                                    return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("@ref"),
                                            IdentifierName($"_{field.Name}")),
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            ThisExpression(),
                                            IdentifierName($"_{field.Name}"))));
                                }
                            }
                            else if (field.IsArray)
                            {
                                if (field.IsFixedArrayOfStruct)
                                {
                                    return GenerateArrayCopyMemory(field, copyFromNative: false);
                                }
                                else
                                {
                                    if (field.MarshalType.QualifiedName == "System.Char")
                                    {
                                        return GenerateCopyMemory(field, false,
                                            BinaryExpression(SyntaxKind.MultiplyExpression,
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName(field.Name),
                                                    IdentifierName("Length")),
                                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(2))
                                            ));
                                    }
                                    else if (field.PublicType.QualifiedName == "System.String")
                                    {
                                        return Block(
                                            LocalDeclarationStatement(
                                                VariableDeclaration(
                                                    ParseTypeName("System.IntPtr"),
                                                    SingletonSeparatedList(
                                                        VariableDeclarator($"{field.Name}_")
                                                            .WithInitializer(EqualsValueClause(
                                                                InvocationExpression(
                                                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                            IdentifierName(Manager.GlobalNamespace.Name),
                                                                            IdentifierName("Utilities")),
                                                                        IdentifierName("StringToHGlobalAnsi")),
                                                                    ArgumentList(SingletonSeparatedList(
                                                                        Argument(
                                                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                                ThisExpression(),
                                                                                IdentifierName(field.Name))))))
                                                ))))),
                                            FixedStatement(
                                                VariableDeclaration(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                                                    SingletonSeparatedList(
                                                        VariableDeclarator("__ptr")
                                                        .WithInitializer(EqualsValueClause(
                                                            PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                    IdentifierName("@ref"),
                                                                    IdentifierName(field.Name))))))),
                                                ExpressionStatement(InvocationExpression(
                                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                            IdentifierName(Manager.GlobalNamespace.Name),
                                                                            IdentifierName("Utilities")),
                                                        IdentifierName("CopyMemory")),
                                                    ArgumentList(
                                                        SeparatedList(
                                                            new[]
                                                            {
                                                                Argument(CastExpression(ParseTypeName("System.IntPtr"), IdentifierName("__ptr"))),
                                                                Argument(IdentifierName($"{field.Name}_")),
                                                                Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                        ThisExpression(),
                                                                        IdentifierName(field.Name)),
                                                                    IdentifierName("Length")))
                                                            }
                                                            )
                                                        )))),
                                            ExpressionStatement(InvocationExpression(
                                                ParseExpression("System.Runtime.InteropServices.Marshal.FreeHGlobal"),
                                                ArgumentList(SingletonSeparatedList(
                                                    Argument(IdentifierName($"{field.Name}_"))))))
                                            );
                                    }
                                    else
                                    {
                                        return GenerateArrayCopyMemory(field, copyFromNative: false);
                                    }
                                }
                            }
                            else
                            {
                                if (field.PublicType.QualifiedName == "System.String")
                                {
                                    return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("@ref"),
                                            IdentifierName(field.Name)),
                                        ConditionalExpression(
                                                BinaryExpression(SyntaxKind.EqualsExpression,
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    ThisExpression(),
                                                    IdentifierName(field.Name)),
                                                LiteralExpression(SyntaxKind.NullLiteralExpression)
                                                ),
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("System"),
                                                        IdentifierName("IntPtr")),
                                                    IdentifierName("Zero")),
                                                InvocationExpression(
                                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                            IdentifierName(Manager.GlobalNamespace.Name),
                                                            IdentifierName("Utilities")),
                                                        IdentifierName("StringToHGlobal" + (field.IsWideChar ? "Uni" : "Ansi"))),
                                                    ArgumentList(SingletonSeparatedList(
                                                        Argument(
                                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                IdentifierName("@ref"),
                                                                IdentifierName(field.Name)))))))));
                                }
                                else if (field.PublicType is CsStruct structType && structType.HasMarshalType)
                                {
                                    StatementSyntax marshalToStatement = ExpressionStatement(InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                ThisExpression(),
                                                IdentifierName(field.Name)),
                                            IdentifierName("__MarshalTo")))
                                    .WithArgumentList(
                                        ArgumentList(
                                            SingletonSeparatedList(
                                                Argument(
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("@ref"),
                                                        IdentifierName(field.Name)))
                                                .WithRefOrOutKeyword(
                                                    Token(SyntaxKind.RefKeyword))))));

                                    return Block(
                                        ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName("@ref"),
                                                IdentifierName(field.Name)),
                                                GetConstructorSyntax(structType)
                                            )),
                                        structType.GenerateAsClass ?
                                            IfStatement(
                                                BinaryExpression(SyntaxKind.NotEqualsExpression,
                                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                        ThisExpression(),
                                                        IdentifierName(field.Name)),
                                                    LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                                marshalToStatement)
                                            : marshalToStatement
                                        );
                                }
                                else
                                {
                                    return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("@ref"),
                                            IdentifierName($"_{field.Name}")),
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            ThisExpression(),
                                            IdentifierName($"_{field.Name}"))));
                                }
                            }
                        }
                        )));
            }
        }

        private static ExpressionSyntax GetConstructorSyntax(CsStruct structType)
        {
            if (structType.HasCustomNew)
            {
                return InvocationExpression(ParseExpression($"{structType.QualifiedName}.__NewNative"));
            }
            else
            {
                return ObjectCreationExpression(ParseTypeName($"{structType.QualifiedName}.__Native"));
            }
        }

        private StatementSyntax GenerateArrayCopyMemory(CsField field, bool copyFromNative) 
            => GenerateCopyMemory(
                field,
                copyFromNative,
                BinaryExpression(SyntaxKind.MultiplyExpression,
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(field.ArrayDimensionValue)),
                    SizeOfExpression(ParseTypeName(field.MarshalType.QualifiedName))
                ));

        private StatementSyntax GenerateCopyMemory(CsField field, bool copyFromNative, ExpressionSyntax numBytesExpression)
        {
            return FixedStatement(
                VariableDeclaration(
                    PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                    SeparatedList(
                        new[]
                        {
                            VariableDeclarator(copyFromNative ? "__to": "__from")
                                .WithInitializer(EqualsValueClause(
                                    PrefixUnaryExpression(
                                        SyntaxKind.AddressOfExpression,
                                        ElementAccessExpression(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                ThisExpression(),
                                                IdentifierName(field.Name)))
                                        .WithArgumentList(
                                            BracketedArgumentList(
                                                SingletonSeparatedList(
                                                    Argument(
                                                        LiteralExpression(
                                                            SyntaxKind.NumericLiteralExpression,
                                                            Literal(0))))))))),
                            VariableDeclarator(copyFromNative ? "__from" : "__to")
                                .WithInitializer(EqualsValueClause(
                                    PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("@ref"),
                                    IdentifierName(field.Name)))))
                        })
                    ),
                ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(Manager.GlobalNamespace.Name),
                                IdentifierName("Utilities")),
                            IdentifierName("CopyMemory")),
                        ArgumentList(
                            SeparatedList(
                                new[]
                                {
                                                            Argument(CastExpression(ParseTypeName("System.IntPtr"), IdentifierName("__to"))),
                                                            Argument(CastExpression(ParseTypeName("System.IntPtr"), IdentifierName("__from"))),
                                                            Argument(numBytesExpression)
                                }
                            )))));
        }

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
        /// <param name="cppElement">The c++ struct.</param>
        /// <returns></returns>
        public override CsStruct Prepare(CppStruct cppStruct)
        {
            // Create a new C# struct
            var nameSpace = Manager.ResolveNamespace(cppStruct);
            var csStruct = new CsStruct(cppStruct)
                                   {
                                       Name = NamingRules.Rename(cppStruct),
                                       // IsFullyMapped to false => The structure is being mapped
                                       IsFullyMapped = false
                                   };

            // Add the C# struct to its namespace
            nameSpace.Add(csStruct);

            // Map the C++ name to the C# struct
            Manager.BindType(cppStruct.Name, csStruct);
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
            bool hasMarshalType = csStruct.HasMarshalType;

            // If this structure need to me moved to another container, move it now
            foreach (var keyValuePair in _mapMoveStructToInner)
            {
                if (keyValuePair.Key.Match(csStruct.CppElementName).Success)
                {
                    string cppName = keyValuePair.Key.Replace(csStruct.CppElementName, keyValuePair.Value);
                    var destSharpStruct = (CsStruct)Manager.FindBindType(cppName);
                    // Remove the struct from his container
                    csStruct.Parent.Remove(csStruct);
                    // Add this struct to the new container struct
                    destSharpStruct.Add(csStruct);
                }
            }

            // Current offset of a field
            int currentFieldAbsoluteOffset = 0;

            // Last field offset
            int previousFieldOffsetIndex = -1;

            // Size of the last field
            int previousFieldSize = 0;

            // 
            int maxSizeOfField = 0;

            bool isInUnion = false;

            int cumulatedBitOffset = 0;


            var inheritedStructs = new Stack<CppStruct>();
            var currentStruct = cppStruct;
            while (currentStruct != null && currentStruct.ParentName != currentStruct.Name)
            {
                inheritedStructs.Push(currentStruct);
                currentStruct = Manager.FindBindType(currentStruct.ParentName)?.CppElement as CppStruct;
            }

            while (inheritedStructs.Count > 0)
            {
                currentStruct = inheritedStructs.Pop();

                int fieldCount = currentStruct.IsEmpty ? 0 : currentStruct.Items.Count;

                // -------------------------------------------------------------------------------
                // Iterate on all fields and perform mapping
                // -------------------------------------------------------------------------------
                for (int fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
                {
                    var cppField = (CppField)currentStruct.Items[fieldIndex];
                    Logger.RunInContext(cppField.ToString(), () =>
                    {
                        var fieldStruct = Manager.GetCsType<CsField>(cppField, true);
                        csStruct.Add(fieldStruct);

                        // Get name
                        fieldStruct.Name = NamingRules.Rename(cppField);

                        // BoolToInt doesn't generate native Marshaling although they have a different marshaller
                        if (!fieldStruct.IsBoolToInt && fieldStruct.HasMarshalType)
                            hasMarshalType = true;


                        // If last field has same offset, then it's a union
                        // CurrentOffset is not moved
                        if (isInUnion && previousFieldOffsetIndex != cppField.Offset)
                        {
                            previousFieldSize = maxSizeOfField;
                            maxSizeOfField = 0;
                            isInUnion = false;
                        }

                        currentFieldAbsoluteOffset += previousFieldSize;
                        var fieldAlignment = (fieldStruct.MarshalType ?? fieldStruct.PublicType).CalculateAlignment();

                        // If field alignment is < 0, then we have a pointer somewhere so we can't align
                        if (fieldAlignment > 0)
                        {
                            // otherwise, align the field on the alignment requirement of the field
                            int delta = (currentFieldAbsoluteOffset % fieldAlignment);
                            if (delta != 0)
                            {
                                currentFieldAbsoluteOffset += fieldAlignment - delta;
                            }
                        }

                        // Get correct offset (for handling union)
                        fieldStruct.Offset = currentFieldAbsoluteOffset;
                        fieldStruct.IsBitField = cppField.IsBitField;

                        // Handle bit fields : calculate BitOffset and BitMask for this field
                        if (previousFieldOffsetIndex != cppField.Offset)
                        {
                            cumulatedBitOffset = 0;
                        }
                        if (cppField.IsBitField)
                        {
                            int lastCumulatedBitOffset = cumulatedBitOffset;
                            cumulatedBitOffset += cppField.BitOffset;
                            fieldStruct.BitMask = ((1 << cppField.BitOffset) - 1);
                            fieldStruct.BitOffset = lastCumulatedBitOffset;
                        }


                        var nextFieldIndex = fieldIndex + 1;
                        if ((previousFieldOffsetIndex == cppField.Offset)
                            || (nextFieldIndex < fieldCount && ((CppField)currentStruct.Items[nextFieldIndex]).Offset == cppField.Offset))
                        {
                            if (previousFieldOffsetIndex != cppField.Offset)
                            {
                                maxSizeOfField = 0;
                            }
                            maxSizeOfField = fieldStruct.SizeOf > maxSizeOfField ? fieldStruct.SizeOf : maxSizeOfField;
                            isInUnion = true;
                            csStruct.ExplicitLayout = true;
                            previousFieldSize = 0;
                        }
                        else
                        {
                            previousFieldSize = fieldStruct.SizeOf;
                        }
                        previousFieldOffsetIndex = cppField.Offset;
                    });
                }
            }

            // In case of explicit layout, check that we can safely generate it on both x86 and x64 (in case of an union
            // using pointers, we can't)
            if (csStruct.ExplicitLayout)
            {
                var fieldList = csStruct.Fields.ToList();
                for(int i = 0; i < fieldList.Count; i++)
                {
                    var field = fieldList[i];
                    var fieldAlignment = (field.MarshalType ?? field.PublicType).CalculateAlignment();

                    if(fieldAlignment < 0)
                    {
                        // If pointer field is not the last one, than we can't handle it
                        if ((i + 1) < fieldList.Count)
                        {
                            Logger.Error(
                                "The field [{0}] in structure [{1}] has pointer alignment within a structure that contains an union. An explicit layout cannot be handled on both x86/x64. This structure needs manual layout (remove fields from definition) and write them manually in xml mapping files",
                                field.CppElementName,
                                csStruct.CppElementName);
                            break;
                        }
                    }
                }
            }

            csStruct.SizeOf = currentFieldAbsoluteOffset + previousFieldSize;
            csStruct.HasMarshalType = hasMarshalType;
        }
    }
}