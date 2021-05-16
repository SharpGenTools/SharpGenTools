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
    internal sealed class StructCodeGenerator : MemberCodeGeneratorBase<CsStruct>
    {
        public override IEnumerable<MemberDeclarationSyntax> GenerateCode(CsStruct csElement)
        {
            var innerStructs = csElement.InnerStructs.SelectMany(GenerateCode);

            var constants = csElement.Variables.SelectMany(var => Generators.Constant.GenerateCode(var));

            var fields = csElement.PublicFields.SelectMany(field =>
            {
                var explicitLayout = !csElement.HasMarshalType && csElement.ExplicitLayout;
                var generator = explicitLayout ? Generators.ExplicitOffsetField : Generators.AutoLayoutField;
                return generator.GenerateCode(field);
            });

            var marshallingStructAndConversions = csElement.HasMarshalType && !csElement.HasCustomMarshal
                                                      ? Generators.NativeStruct.GenerateCode(csElement)
                                                      : Enumerable.Empty<MemberDeclarationSyntax>();

            var attributeList = !csElement.HasMarshalType
                                    ? SingletonList(NativeStructCodeGenerator.GenerateStructLayoutAttribute(csElement))
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

            yield return AddDocumentationTrivia(declaration, csElement);
        }

        public StructCodeGenerator(Ioc ioc) : base(ioc)
        {
        }
    }
}
