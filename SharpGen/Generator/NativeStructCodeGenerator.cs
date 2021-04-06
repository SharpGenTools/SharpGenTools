using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    internal sealed class NativeStructCodeGenerator : IMultiCodeGenerator<CsStruct, MemberDeclarationSyntax>
    {
        private readonly IGeneratorRegistry generators;

        public NativeStructCodeGenerator(IGeneratorRegistry generators, GlobalNamespaceProvider globalNamespace)
        {
            this.generators = generators ?? throw new ArgumentNullException(nameof(generators));
        }

        public IEnumerable<MemberDeclarationSyntax> GenerateCode(CsStruct csStruct)
        {
            var marshalStruct = csStruct.RoslynNative.WithIdentifier(Identifier("__Native"))
                               .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.PartialKeyword)));

            yield return marshalStruct;

            yield return GenerateMarshalFree(csStruct);

            yield return GenerateMarshalFrom(csStruct);

            yield return GenerateMarshalTo(csStruct);
        }

        private StatementSyntax GenerateMarshalFreeForField(CsMarshalBase field) =>
            generators.Marshalling.GetMarshaller(field)?.GenerateNativeCleanup(field, false);

        private MethodDeclarationSyntax GenerateMarshalFree(CsStruct csStruct) => GenerateMarshalMethod(
            "__MarshalFree",
            Block(
                csStruct.Fields
                        .Where(field => !field.IsArray)
                        .Select(GenerateMarshalFreeForField)
                        .Where(statement => statement != null)
            )
        );

        private MethodDeclarationSyntax GenerateMarshalTo(CsStruct csStruct)
        {
            IEnumerable<StatementSyntax> FieldMarshallers(CsField field)
            {
                if ((field.Relations?.Count ?? 0) == 0)
                {
                    yield return generators.Marshalling.GetMarshaller(field).GenerateManagedToNative(field, false);
                    yield break;
                }

                foreach (var relation in field.Relations)
                {
                    var marshaller = generators.Marshalling.GetRelationMarshaller(relation);
                    CsField publicElement = null;

                    if (relation is LengthRelation related)
                    {
                        var relatedMarshallableName = related.Identifier;

                        publicElement = csStruct.Fields.First(fld => fld.CppElementName == relatedMarshallableName);
                    }

                    yield return marshaller.GenerateManagedToNative(publicElement, field);
                }
            }

            return GenerateMarshalMethod(
                "__MarshalTo",
                Block(
                    csStruct.Fields.SelectMany(FieldMarshallers)
                            .Where(statement => statement != null)
                )
            );
        }

        private static ParameterListSyntax MarshalParameterListSyntax => ParameterList(
            SingletonSeparatedList(Parameter(Identifier("@ref")).WithType(RefType(ParseTypeName("__Native"))))
        );

        private static MethodDeclarationSyntax GenerateMarshalMethod(string name, BlockSyntax body)
        {
            return MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), name)
                  .WithParameterList(MarshalParameterListSyntax)
                  .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.UnsafeKeyword)))
                  .WithBody(Block(body));
        }

        private MethodDeclarationSyntax GenerateMarshalFrom(CsStruct csStruct) => GenerateMarshalMethod(
            "__MarshalFrom",
            Block(
                csStruct.Fields
                        .Where(field => (field.Relations?.Count ?? 0) == 0)
                        .Select(field =>
                                    generators.Marshalling.GetMarshaller(field)
                                              .GenerateNativeToManaged(field, false))
                        .Where(statement => statement != null)
            )
        );
    }
}
