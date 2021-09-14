using System;
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
    internal sealed class InterfaceCodeGenerator : MemberMultiCodeGeneratorBase<CsInterface>
    {
        private static readonly XmlTextSyntax XmlDocStartSyntax = XmlText(
            XmlTextLiteral(TriviaList(DocumentationCommentExterior("///")), " ", " ", TriviaList())
        );

        private static readonly XmlTextSyntax XmlDocEndSyntax = XmlText(XmlTextNewLine("\n", false));
        private static readonly XmlNameSyntax XmlDocSummaryName = XmlName(Identifier("summary"));
        private static readonly XmlElementStartTagSyntax XmlDocSummaryStartSyntax = XmlElementStartTag(XmlDocSummaryName);
        private static readonly XmlElementEndTagSyntax XmlDocSummaryEndSyntax = XmlElementEndTag(XmlDocSummaryName);

        private static readonly SyntaxTokenList DisposeCoreModifierList = TokenList(
            Token(
                TriviaList(
                    Trivia(
                        DocumentationCommentTrivia(
                            SyntaxKind.SingleLineDocumentationCommentTrivia,
                            List(
                                new XmlNodeSyntax[]
                                {
                                    XmlDocStartSyntax,
                                    XmlEmptyElement("inheritdoc"),
                                    XmlDocEndSyntax
                                }
                            )
                        )
                    )
                ),
                SyntaxKind.ProtectedKeyword,
                TriviaList()
            ),
            Token(SyntaxKind.OverrideKeyword)
        );

        private static readonly ParameterListSyntax DisposeCoreParameterList = ParameterList(
            SeparatedList(
                new[]
                {
                    Parameter(Identifier("nativePointer")).WithType(GeneratorHelpers.IntPtrType),
                    Parameter(Identifier("disposing")).WithType(PredefinedType(Token(SyntaxKind.BoolKeyword)))
                }
            )
        );

        private static readonly ParameterListSyntax NativePointerUpdatedParameterList = ParameterList(
            SeparatedList(
                new[]
                {
                    Parameter(Identifier("oldNativePointer")).WithType(GeneratorHelpers.IntPtrType)
                }
            )
        );

        private static readonly SyntaxToken DisposeCoreIdentifier = Identifier("DisposeCore");
        private static readonly SyntaxToken NativePointerUpdatedIdentifier = Identifier("NativePointerUpdated");

        private static readonly ExpressionStatementSyntax BaseDisposeCoreCallStatement = ExpressionStatement(
            InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    BaseExpression(), IdentifierName(DisposeCoreIdentifier)
                ),
                ArgumentList(
                    SeparatedList(
                        new[]
                        {
                            Argument(IdentifierName("nativePointer")),
                            Argument(IdentifierName("disposing"))
                        }
                    )
                )
            )
        );

        private static readonly IdentifierNameSyntax OldNativePointerIdentifierName = IdentifierName("oldNativePointer");

        private static readonly ExpressionStatementSyntax BaseNativePointerUpdatedCallStatement = ExpressionStatement(
            InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    BaseExpression(), IdentifierName(NativePointerUpdatedIdentifier)
                ),
                ArgumentList(SingletonSeparatedList(Argument(OldNativePointerIdentifierName)))
            )
        );

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

            var members = NewMemberList;

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
                            NullLiteral,
                            ObjectCreationExpression(IdentifierName(csElement.Name))
                               .WithArgumentList(
                                    ArgumentList(SingletonSeparatedList(Argument(IdentifierName(nativePtr))))
                                ))))
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
            }

            members.AddRange(csElement.ExpressionConstants, Generators.ExpressionConstant);
            members.AddRange(csElement.GuidConstants, Generators.GuidConstant);
            members.AddRange(csElement.ResultConstants, Generators.ResultConstant);

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
                            NativePointerUpdatedIdentifier
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
                members.AddRange(csElement.Properties, Generators.Property);

                if (csElement.AutoDisposePersistentProperties && csElement.Properties.Any(x => x.IsDisposeBlockNeeded))
                {
                    StatementSyntaxList nativePointerUpdated = new(), nativePointerUpdatedConditional = new();

                    IEnumerable<CsProperty> GetDisposableProperties() =>
                        csElement.Properties.Where(x => x.IsDisposeBlockNeeded);

                    StatementSyntax GetDisposeInvocation(CsProperty x, params ArgumentSyntax[] args) =>
                        ExpressionStatement(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    GlobalNamespace.GetTypeNameSyntax(WellKnownName.MemoryHelpers),
                                    GenericName(Identifier("Dispose"))
                                       .WithTypeArgumentList(
                                            TypeArgumentList(
                                                SingletonSeparatedList(
                                                    ParseTypeName(x.PublicType.QualifiedName)
                                                )
                                            )
                                        )
                                ),
                                ArgumentList(SeparatedList(args))
                            )
                        );

                    nativePointerUpdated.Add(BaseNativePointerUpdatedCallStatement);
                    nativePointerUpdatedConditional.AddRange(
                        GetDisposableProperties(),
                        x => GetDisposeInvocation(
                            x,
                            Argument(IdentifierName(x.PersistentFieldIdentifier))
                               .WithRefKindKeyword(Token(SyntaxKind.RefKeyword)),
                            Argument(LiteralExpression(SyntaxKind.TrueLiteralExpression))
                        )
                    );
                    nativePointerUpdated.Add(
                        IfStatement(
                            BinaryExpression(
                                SyntaxKind.NotEqualsExpression,
                                OldNativePointerIdentifierName, GeneratorHelpers.IntPtrZero
                            ),
                            nativePointerUpdatedConditional.ToStatement()
                        )
                    );

                    members.Add(
                        MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), NativePointerUpdatedIdentifier)
                           .WithModifiers(DisposeCoreModifierList)
                           .WithParameterList(NativePointerUpdatedParameterList)
                           .WithBody(nativePointerUpdated.ToBlock())
                    );
                }
            }

            members.AddRange(csElement.Methods, Generators.Method);

            var resultList = NewMemberList;

            if (csElement.IsCallback && csElement.AutoGenerateShadow)
            {
                resultList.Add(csElement, Generators.Shadow);
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

            resultList.Add(AddDocumentationTrivia(declaration, csElement));
            return resultList;
        }


        private IEnumerable<StatementSyntax> GenerateUpdateInnerInterface(CsInterface csInterface)
        {
            yield return IfStatement(BinaryExpression(SyntaxKind.EqualsExpression,
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                ThisExpression(),
                IdentifierName(csInterface.PropertyAccessName)),
                NullLiteral),
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
