using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    internal sealed class StructCodeGenerator : MemberSingleCodeGeneratorBase<CsStruct>
    {
        public StructCodeGenerator(Ioc ioc) : base(ioc)
        {
        }

        public override MemberDeclarationSyntax GenerateCode(CsStruct csElement)
        {
            var list = NewMemberList;
            list.AddRange(csElement.InnerStructs, Generators.Struct);
            list.AddRange(csElement.ExpressionConstants, Generators.ExpressionConstant);
            list.AddRange(csElement.GuidConstants, Generators.GuidConstant);
            list.AddRange(csElement.ResultConstants, Generators.ResultConstant);

            var explicitLayout = !csElement.HasMarshalType && csElement.ExplicitLayout;
            var generator = explicitLayout ? Generators.ExplicitOffsetField : Generators.AutoLayoutField;

            list.AddRange(csElement.PublicFields, generator);

            if (csElement.HasMarshalType && !csElement.HasCustomMarshal)
                list.Add(csElement, Generators.NativeStruct);

            var attributeList = !csElement.HasMarshalType
                                    ? SingletonList(NativeStructCodeGenerator.GenerateStructLayoutAttribute(csElement))
                                    : default;

            var modifierTokenList = csElement.VisibilityTokenList.Add(Token(SyntaxKind.PartialKeyword));
            var identifier = Identifier(csElement.Name);

            MemberDeclarationSyntax declaration = csElement.GenerateAsClass
                                                      ? ClassDeclaration(
                                                          attributeList,
                                                          modifierTokenList,
                                                          identifier,
                                                          default,
                                                          default,
                                                          default,
                                                          List(list)
                                                      )
                                                      : StructDeclaration(
                                                          attributeList,
                                                          modifierTokenList,
                                                          identifier,
                                                          default,
                                                          default,
                                                          default,
                                                          List(list)
                                                      );

            return AddDocumentationTrivia(declaration, csElement);
        }
    }
}