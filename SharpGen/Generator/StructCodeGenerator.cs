using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using SharpGen.Transform;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    class StructCodeGenerator : MemberCodeGeneratorBase<CsStruct>
    {
        public StructCodeGenerator(IGeneratorRegistry generators, IDocumentationLinker documentation, ExternalDocCommentsReader docReader)
            : base(documentation, docReader)
        {
            Generators = generators;
        }

        public IGeneratorRegistry Generators { get; }

        public override IEnumerable<MemberDeclarationSyntax> GenerateCode(CsStruct csElement)
        {
            var documentationTrivia = GenerateDocumentationTrivia(csElement);
            var layoutKind = csElement.ExplicitLayout ? "Explicit" : "Sequential";
            var structLayoutAttribute = Attribute(ParseName("System.Runtime.InteropServices.StructLayoutAttribute"))
                    .WithArgumentList(
                        AttributeArgumentList(SeparatedList(
                            new []
                            {
                                AttributeArgument(ParseName($"System.Runtime.InteropServices.LayoutKind.{layoutKind}")),
                                AttributeArgument(
                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(csElement.Align)))
                                    .WithNameEquals(NameEquals(IdentifierName("Pack"))),
                                AttributeArgument(
                                    ParseName("System.Runtime.InteropServices.CharSet.Unicode")
                                ).WithNameEquals(NameEquals(IdentifierName("CharSet")))
                            }
                        )
                    )
                );

            var innerStructs = csElement.InnerStructs.SelectMany(GenerateCode);

            var constants = csElement.Variables.SelectMany(var => Generators.Constant.GenerateCode(var));

            var fields = csElement.Fields.Where(field => field.Relations.Count == 0).SelectMany(field =>
            {
                var explicitLayout = !csElement.HasMarshalType && csElement.ExplicitLayout;
                var generator = explicitLayout ? Generators.ExplicitOffsetField : Generators.AutoLayoutField;
                return generator.GenerateCode(field);
            });

            var marshallingStructAndConversions = Enumerable.Empty<MemberDeclarationSyntax>();

            if (csElement.HasMarshalType && !csElement.HasCustomMarshal)
            {
                marshallingStructAndConversions = Generators.NativeStruct.GenerateCode(csElement);
            }

            var attributeList = !csElement.HasMarshalType
                                    ? SingletonList(AttributeList(SingletonSeparatedList(structLayoutAttribute)))
                                    : default;
            var modifierTokenList = csElement.VisibilityTokenList.Add(Token(SyntaxKind.PartialKeyword));
            var identifier = Identifier(csElement.Name);
            var memberList = List(
                innerStructs.Concat(constants).Concat(fields).Concat(marshallingStructAndConversions)
            );

            MemberDeclarationSyntax declaration = csElement.GenerateAsClass
                                                      ? ClassDeclaration(
                                                          attributeList,
                                                          modifierTokenList,
                                                          identifier,
                                                          default,
                                                          default,
                                                          default,
                                                          memberList
                                                      )
                                                      : StructDeclaration(
                                                          attributeList,
                                                          modifierTokenList,
                                                          identifier,
                                                          default,
                                                          default,
                                                          default,
                                                          memberList
                                                      );

            yield return declaration.WithLeadingTrivia(Trivia(documentationTrivia));
        }
    }
}
