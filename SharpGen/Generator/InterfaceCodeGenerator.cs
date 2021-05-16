using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Logging;
using SharpGen.Model;
using SharpGen.Transform;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    internal sealed class InterfaceCodeGenerator : MemberCodeGeneratorBase<CsInterface>
    {
        private static readonly XmlTextSyntax XmlDocStartSyntax = XmlText(
            XmlTextLiteral(TriviaList(DocumentationCommentExterior("///")), " ", " ", TriviaList())
        );

        private static readonly XmlTextSyntax XmlDocEndSyntax = XmlText(XmlTextNewLine("\n", false));
        private static readonly XmlNameSyntax XmlDocSummaryName = XmlName(Identifier("summary"));
        private static readonly XmlElementStartTagSyntax XmlDocSummaryStartSyntax = XmlElementStartTag(XmlDocSummaryName);
        private static readonly XmlElementEndTagSyntax XmlDocSummaryEndSyntax = XmlElementEndTag(XmlDocSummaryName);

        public override IEnumerable<MemberDeclarationSyntax> GenerateCode(CsInterface csElement)
        {
            AttributeListSyntax attributes = null;
            if (csElement.Guid != null)
            {
                attributes = AttributeList(SingletonSeparatedList(Attribute(ParseName("System.Runtime.InteropServices.GuidAttribute"),
                    AttributeArgumentList(SingletonSeparatedList(
                        AttributeArgument(
                            LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(csElement.Guid))))))));
            }

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

            var nativePtr = Identifier("nativePtr");

            if (!csElement.IsCallback)
            {
                ExpressionStatementSyntax InnerInterfaceSelector(CsInterface inner) =>
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(inner.PropertyAccessName),
                            ObjectCreationExpression(ParseTypeName(inner.QualifiedName))
                               .WithArgumentList(
                                    ArgumentList(SingletonSeparatedList(Argument(IdentifierName(nativePtr))))
                                )));

                members.Add(
                    ConstructorDeclaration(
                        default,
                        TokenList(Token(SyntaxKind.PublicKeyword)),
                        Identifier(csElement.Name),
                        ParameterList(
                            SingletonSeparatedList(Parameter(nativePtr).WithType(GeneratorHelpers.IntPtrType))
                        ),
                        ConstructorInitializer(
                            SyntaxKind.BaseConstructorInitializer,
                            ArgumentList(SingletonSeparatedList(Argument(IdentifierName(nativePtr))))
                        ),
                        Block(List(csElement.InnerInterfaces.Select(InnerInterfaceSelector)))
                    ));

                members.Add(ConversionOperatorDeclaration(
                    default,
                    TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)),
                    Token(SyntaxKind.ExplicitKeyword),
                    IdentifierName(csElement.Name),
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(nativePtr).WithType(GeneratorHelpers.IntPtrType)
                        )),
                    default,
                    ArrowExpressionClause(
                        ConditionalExpression(
                            BinaryExpression(
                                SyntaxKind.EqualsExpression,
                                IdentifierName(nativePtr),
                                GeneratorHelpers.IntPtrZero
                            ),
                            LiteralExpression(SyntaxKind.NullLiteralExpression),
                            ObjectCreationExpression(IdentifierName(csElement.Name))
                               .WithArgumentList(
                                    ArgumentList(SingletonSeparatedList(Argument(IdentifierName(nativePtr))))
                                ))))
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
            }

            members.AddRange(csElement.Variables.SelectMany(var => Generators.Constant.GenerateCode(var)));

            if (csElement.HasInnerInterfaces)
            {
                static SyntaxTrivia GenerateXmlDoc(string value) => Trivia(
                    DocumentationCommentTrivia(
                        SyntaxKind.SingleLineDocumentationCommentTrivia,
                        List(
                            new XmlNodeSyntax[]
                            {
                                XmlDocStartSyntax,
                                XmlElement(
                                    XmlDocSummaryStartSyntax,
                                    SingletonList<XmlNodeSyntax>(XmlText(XmlTextLiteral(value))),
                                    XmlDocSummaryEndSyntax
                                ),
                                XmlDocEndSyntax
                            }
                        )
                    )
                );

                members.Add(
                    MethodDeclaration(
                            PredefinedType(Token(SyntaxKind.VoidKeyword)),
                            Identifier("NativePointerUpdated")
                        )
                       .WithModifiers(TokenList(Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.OverrideKeyword)))
                       .WithParameterList(
                            ParameterList(
                                SingletonSeparatedList(
                                    Parameter(Identifier("oldPointer")).WithType(GeneratorHelpers.IntPtrType)
                                )))
                       .WithBody(
                            Block(
                                SingletonList<StatementSyntax>(
                                        ExpressionStatement(
                                            InvocationExpression(
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        BaseExpression(),
                                                        IdentifierName("NativePointerUpdated")
                                                    ))
                                               .WithArgumentList(
                                                    ArgumentList(
                                                        SingletonSeparatedList(Argument(IdentifierName("oldPointer")))
                                                    ))))
                                   .AddRange(
                                        csElement.InnerInterfaces.SelectMany(GenerateUpdateInnerInterface)
                                    )))
                       .WithLeadingTrivia(GenerateXmlDoc("Update nested inner interfaces pointer"))
                );

                PropertyDeclarationSyntax InnerInterfaceSelector(CsInterface iface) =>
                    PropertyDeclaration(IdentifierName(iface.QualifiedName), Identifier(iface.PropertyAccessName))
                       .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                       .WithAccessorList(AccessorList(List(new[]
                        {
                            AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                               .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                            AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                               .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)))
                               .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                        })))
                       .WithLeadingTrivia(GenerateXmlDoc($"Inner interface giving access to {iface.Name} methods."));

                members.AddRange(csElement.InnerInterfaces.Select(InnerInterfaceSelector));
            }

            if (!csElement.IsCallback)
            {
                members.AddRange(csElement.Properties.SelectMany(prop => Generators.Property.GenerateCode(prop)));
            }

            foreach (var method in csElement.Methods)
            {
                members.AddRange(Generators.Method.GenerateCode(method));
            }

            if (csElement.IsCallback && csElement.AutoGenerateShadow)
            {
                yield return Generators.Shadow.GenerateCode(csElement);
                var shadowAttribute = Attribute(GlobalNamespace.GetTypeNameSyntax(WellKnownName.ShadowAttribute))
                    .AddArgumentListArguments(AttributeArgument(TypeOfExpression(ParseTypeName(csElement.ShadowName))));
                attributes = attributes?.AddAttributes(shadowAttribute) ?? AttributeList(SingletonSeparatedList(shadowAttribute));
            }

            var attributeList = attributes != null ? SingletonList(attributes) : default;

            var modifiers = csElement.VisibilityTokenList
                .Add(Token(SyntaxKind.PartialKeyword));

            MemberDeclarationSyntax declaration = csElement.IsCallback
                                                      ? InterfaceDeclaration(
                                                          attributeList, modifiers, Identifier(csElement.Name),
                                                          default, baseList, default, List(members)
                                                      )
                                                      : ClassDeclaration(
                                                          attributeList, modifiers, Identifier(csElement.Name),
                                                          default, baseList, default, List(members)
                                                      );

            yield return AddDocumentationTrivia(declaration, csElement);
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
                                ArgumentList(SingletonSeparatedList(Argument(GeneratorHelpers.IntPtrZero)))
                            ))));

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

        public InterfaceCodeGenerator(Ioc ioc) : base(ioc)
        {
        }
    }
}
