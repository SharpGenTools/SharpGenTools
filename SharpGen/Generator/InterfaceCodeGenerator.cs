using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Microsoft.CodeAnalysis;
using SharpGen.Transform;

namespace SharpGen.Generator
{
    class InterfaceCodeGenerator : MemberCodeGeneratorBase<CsInterface>
    {
        private readonly GlobalNamespaceProvider globalNamespace;

        public InterfaceCodeGenerator(IGeneratorRegistry generators, IDocumentationLinker documentation, ExternalDocCommentsReader docReader, GlobalNamespaceProvider globalNamespace)
            : base(documentation, docReader)
        {
            Generators = generators;
            this.globalNamespace = globalNamespace;
        }

        public IGeneratorRegistry Generators { get; }

        public override IEnumerable<MemberDeclarationSyntax> GenerateCode(CsInterface csElement)
        {
            var docComment = GenerateDocumentationTrivia(csElement);

            AttributeListSyntax attributes = null;
            if (csElement.Guid != null)
            {
                attributes = AttributeList(SingletonSeparatedList(Attribute(ParseName("System.Runtime.InteropServices.GuidAttribute"),
                    AttributeArgumentList(SingletonSeparatedList(
                        AttributeArgument(
                            LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(csElement.Guid))))))));
            }

            var visibility = TokenList(ParseTokens(csElement.VisibilityName)
                .Concat(new[] { Token(SyntaxKind.PartialKeyword) }));

            var baseList = default(BaseListSyntax);

            if (csElement.Base != null || csElement.IBase != null)
            {
                baseList = BaseList();
                if (csElement.Base != null)
                {
                    baseList = baseList.AddTypes(SimpleBaseType(ParseTypeName(csElement.Base.QualifiedName)));
                }
                if (csElement.IBase != null)
                {
                    baseList = baseList.AddTypes(SimpleBaseType(ParseTypeName(csElement.IBase.QualifiedName)));
                }
            }

            var members = new List<MemberDeclarationSyntax>();

            if (!csElement.IsCallback)
            {
                members.Add(ConstructorDeclaration(
                    default,
                    TokenList(Token(SyntaxKind.PublicKeyword)),
                    Identifier(csElement.Name),
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(
                                Identifier("nativePtr"))
                            .WithType(
                                QualifiedName(
                                    IdentifierName("System"),
                                    IdentifierName("IntPtr"))))),
                        ConstructorInitializer(
                        SyntaxKind.BaseConstructorInitializer,
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    IdentifierName("nativePtr"))))),
                                    Block(
                                        List(
                                            csElement.InnerInterfaces.Select(inner =>
                                                ExpressionStatement(
                                                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                        IdentifierName(inner.PropertyAccessName),
                                                        ObjectCreationExpression(
                                                            ParseTypeName(inner.QualifiedName))
                                                            .WithArgumentList(ArgumentList(
                                                                SingletonSeparatedList(
                                                                    Argument(IdentifierName("nativePtr"))
                                        )))))))
                                    )));

                members.Add(ConversionOperatorDeclaration(
                    default,
                    TokenList(
                        new[]{
                            Token(SyntaxKind.PublicKeyword),
                            Token(SyntaxKind.StaticKeyword)}),
                    Token(SyntaxKind.ExplicitKeyword),
                    IdentifierName(csElement.Name),
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(
                                Identifier("nativePtr"))
                            .WithType(
                                QualifiedName(
                                    IdentifierName("System"),
                                    IdentifierName("IntPtr"))))),
                    default,
                    ArrowExpressionClause(
                        ConditionalExpression(
                            BinaryExpression(
                                SyntaxKind.EqualsExpression,
                                IdentifierName("nativePtr"),
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("System"),
                                        IdentifierName("IntPtr")),
                                    IdentifierName("Zero"))),
                            LiteralExpression(
                                SyntaxKind.NullLiteralExpression),
                            ObjectCreationExpression(
                                IdentifierName(csElement.Name))
                            .WithArgumentList(
                                ArgumentList(
                                    SingletonSeparatedList(
                                        Argument(
                                            IdentifierName("nativePtr"))))))))
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
            }

            members.AddRange(csElement.Variables.SelectMany(var => Generators.Constant.GenerateCode(var)));

            if (csElement.HasInnerInterfaces)
            {
                members.Add(MethodDeclaration(
                    PredefinedType(
                        Token(SyntaxKind.VoidKeyword)),
                    Identifier("NativePointerUpdated"))
                .WithModifiers(
                    TokenList(
                        new[]
                        {
                            Token(SyntaxKind.ProtectedKeyword),
                            Token(SyntaxKind.OverrideKeyword)
                        }))
                .WithParameterList(
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(
                                Identifier("oldPointer"))
                            .WithType(
                                QualifiedName(
                                    IdentifierName("System"),
                                    IdentifierName("IntPtr"))))))
                .WithBody(
                    Block(
                        SingletonList<StatementSyntax>(
                            ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        BaseExpression(),
                                        IdentifierName("NativePointerUpdated")))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(
                                                IdentifierName("oldPointer")))))))
                        .AddRange(
                            csElement.InnerInterfaces.SelectMany(csInterface => GenerateUpdateInnerInterface(csInterface))
                            )))
                .WithLeadingTrivia(Trivia(DocumentationCommentTrivia(
                        SyntaxKind.SingleLineDocumentationCommentTrivia,
                        List(
                            new XmlNodeSyntax[]{
                                    XmlText(XmlTextNewLine("", true)),
                                    XmlElement(
                                        XmlElementStartTag(XmlName(Identifier("summary"))),
                                        SingletonList<XmlNodeSyntax>(
                                            XmlText(XmlTextLiteral("Update nested inner interfaces pointer"))),
                                        XmlElementEndTag(XmlName(Identifier("summary")))
                                    ),
                                    XmlText(XmlTextNewLine("\n", false))
                            })))));

                members.AddRange(csElement.InnerInterfaces.Select(iface => PropertyDeclaration(
                    IdentifierName(iface.QualifiedName),
                    Identifier(iface.PropertyAccessName))
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword)))
                .WithAccessorList(
                    AccessorList(
                        List(
                            new[]{
                                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                .WithSemicolonToken(
                                    Token(SyntaxKind.SemicolonToken)),
                                AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                .WithModifiers(
                                    TokenList(
                                        Token(SyntaxKind.PrivateKeyword)))
                                .WithSemicolonToken(
                                    Token(SyntaxKind.SemicolonToken))})))
                .WithLeadingTrivia(Trivia(DocumentationCommentTrivia(
                        SyntaxKind.SingleLineDocumentationCommentTrivia,
                        List(
                            new XmlNodeSyntax[]{
                                    XmlText(XmlTextNewLine("", true)),
                                    XmlElement(
                                        XmlElementStartTag(XmlName(Identifier("summary"))),
                                        SingletonList<XmlNodeSyntax>(
                                            XmlText(XmlTextLiteral($"Inner interface giving access to {iface.Name} methods."))),
                                        XmlElementEndTag(XmlName(Identifier("summary")))
                                    ),
                                    XmlText(XmlTextNewLine("\n", false))
                            }))))));
            }

            if (!csElement.IsCallback)
            {
                members.AddRange(csElement.Properties.SelectMany(prop => Generators.Property.GenerateCode(prop)));
            }

            foreach (var method in csElement.Methods)
            {
                method.Hidden = csElement.IsCallback && !csElement.AutoGenerateShadow;
                method.SignatureOnly = csElement.IsCallback;
                members.AddRange(Generators.Method.GenerateCode(method));
            }

            if (csElement.IsCallback && csElement.AutoGenerateShadow)
            {
                yield return Generators.Shadow.GenerateCode(csElement);
                var shadowAttribute = Attribute(globalNamespace.GetTypeNameSyntax(WellKnownName.ShadowAttribute))
                    .AddArgumentListArguments(AttributeArgument(TypeOfExpression(ParseTypeName(csElement.ShadowName))));
                attributes = attributes?.AddAttributes(shadowAttribute) ?? AttributeList(SingletonSeparatedList(shadowAttribute));
            }

            yield return csElement.IsCallback ?
                (MemberDeclarationSyntax)InterfaceDeclaration(attributes != null ? SingletonList(attributes) : default,
                TokenList(ParseTokens(csElement.VisibilityName)).Add(Token(SyntaxKind.PartialKeyword)),
                Identifier(csElement.Name),
                default, baseList, default, List(members))
                :
                ClassDeclaration(attributes != null ? SingletonList(attributes) : default,
                TokenList(ParseTokens(csElement.VisibilityName)).Add(Token(SyntaxKind.PartialKeyword)),
                Identifier(csElement.Name),
                default, baseList, default, List(members))
                ;
        }


        private IEnumerable<StatementSyntax> GenerateUpdateInnerInterface(CsInterface csInterface)
        {
            yield return IfStatement(BinaryExpression(SyntaxKind.EqualsExpression,
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                ThisExpression(),
                IdentifierName(csInterface.PropertyAccessName)),
                LiteralExpression(SyntaxKind.NullLiteralExpression)),
                ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                ThisExpression(),
                IdentifierName(csInterface.PropertyAccessName)),
                        ObjectCreationExpression(ParseTypeName(csInterface.QualifiedName))
                        .WithArgumentList(
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("System"),
                                            IdentifierName("IntPtr")),
                                        IdentifierName("Zero")))))))));

            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ThisExpression(),
                            IdentifierName(csInterface.PropertyAccessName)),
                        IdentifierName("NativePointer")),
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ThisExpression(),
                        IdentifierName("NativePointer"))));
        }

    }
}
