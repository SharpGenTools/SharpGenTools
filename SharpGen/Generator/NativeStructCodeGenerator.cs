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
        private readonly GlobalNamespaceProvider globalNamespace;

        public NativeStructCodeGenerator(IGeneratorRegistry generators, GlobalNamespaceProvider globalNamespace)
        {
            this.generators = generators ?? throw new ArgumentNullException(nameof(generators));
            this.globalNamespace = globalNamespace ?? throw new ArgumentNullException(nameof(globalNamespace));
        }

        public IEnumerable<MemberDeclarationSyntax> GenerateCode(CsStruct csElement)
        {
            var layoutKind = csElement.ExplicitLayout ? "Explicit" : "Sequential";
            var structLayoutAttribute = Attribute(ParseName("System.Runtime.InteropServices.StructLayoutAttribute"))
               .WithArgumentList(
                   AttributeArgumentList(SeparatedList(
                       new[] {
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
            return GenerateMarshallingStructAndConversions(csElement, AttributeList(SingletonSeparatedList(structLayoutAttribute)));
        }
        private IEnumerable<MemberDeclarationSyntax> GenerateMarshallingStructAndConversions(CsStruct csStruct, AttributeListSyntax structLayoutAttributeList)
        {
            var marshalStruct = StructDeclaration("__Native")
                               .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.PartialKeyword)))
                               .WithAttributeLists(SingletonList(structLayoutAttributeList))
                               .WithMembers(List(csStruct.Fields.SelectMany(GenerateMarshalStructField)));

            yield return marshalStruct;

            yield return GenerateMarshalFree(csStruct);

            yield return GenerateMarshalFrom(csStruct);

            yield return GenerateMarshalTo(csStruct);

            IEnumerable<MemberDeclarationSyntax> GenerateMarshalStructField(CsField field)
            {
                var fieldDecl = FieldDeclaration(VariableDeclaration(ParseTypeName(field.MarshalType.QualifiedName)))
                   .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)));

                if (csStruct.ExplicitLayout)
                {
                    fieldDecl = fieldDecl.WithAttributeLists(
                        SingletonList(
                            AttributeList(
                                SingletonSeparatedList(
                                    Attribute(
                                        ParseName("System.Runtime.InteropServices.FieldOffset"),
                                        AttributeArgumentList(
                                            SingletonSeparatedList(
                                                AttributeArgument(
                                                    LiteralExpression(
                                                        SyntaxKind.NumericLiteralExpression,
                                                        Literal(field.Offset))))))))
                        ));
                }

                if (field.IsArray)
                {
                    yield return fieldDecl.WithDeclaration(
                        fieldDecl.Declaration.AddVariables(VariableDeclarator(field.Name))
                    );

                    for (var i = 1; i < field.ArrayDimensionValue; i++)
                    {
                        var declaration = fieldDecl.WithDeclaration(
                            fieldDecl.Declaration.AddVariables(VariableDeclarator($"__{field.Name}{i}"))
                        );

                        if (csStruct.ExplicitLayout)
                        {
                            var offset = field.Offset + (field.Size / field.ArrayDimensionValue) * i;
                            declaration = declaration.WithAttributeLists(
                                SingletonList(
                                    AttributeList(
                                        SingletonSeparatedList(
                                            Attribute(
                                                ParseName("System.Runtime.InteropServices.FieldOffset"),
                                                AttributeArgumentList(
                                                    SingletonSeparatedList(
                                                        AttributeArgument(
                                                            LiteralExpression(
                                                                SyntaxKind.NumericLiteralExpression,
                                                                Literal(offset))))))))
                                ));
                        }

                        yield return declaration;
                    }
                }
                else if (field.HasNativeValueType)
                {
                    yield return fieldDecl.WithDeclaration(
                        VariableDeclaration(
                            ParseTypeName($"{field.MarshalType.QualifiedName}.__Native"),
                            SingletonSeparatedList(VariableDeclarator(field.Name))
                        )
                    );
                }
                else
                {
                    yield return fieldDecl.WithDeclaration(
                        fieldDecl.Declaration.AddVariables(VariableDeclarator(field.Name))
                    );
                }
            }
        }

        private StatementSyntax GenerateMarshalFreeForField(CsMarshalBase field) =>
            generators.Marshalling.GetMarshaller(field)?.GenerateNativeCleanup(field, false);

        private MethodDeclarationSyntax GenerateMarshalFree(CsStruct csStruct) =>
            MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), "__MarshalFree")
               .WithParameterList(
                    ParameterList(SingletonSeparatedList(
                                      Parameter(Identifier("@ref")).WithType(RefType(ParseTypeName("__Native")))))
                )
               .WithBody(
                    Block(
                        List(
                            csStruct.Fields
                                    .Where(field => !field.IsArray)
                                    .Select(GenerateMarshalFreeForField)
                                    .Where(statement => statement != null)
                        )
                    )
                )
               .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.UnsafeKeyword)));

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

            return MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), "__MarshalTo")
                .WithParameterList(ParameterList(SingletonSeparatedList(
                    Parameter(Identifier("@ref")).WithType(RefType(ParseTypeName("__Native"))))))
                .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.UnsafeKeyword)))
                .WithBody(
                    Block(
                        csStruct.Fields.SelectMany(FieldMarshallers).Where(statement => statement != null)
                    )
                );
        }

        private MethodDeclarationSyntax GenerateMarshalFrom(CsStruct csStruct)
        {
            return MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), "__MarshalFrom")
                .WithParameterList(ParameterList(SingletonSeparatedList(
                    Parameter(Identifier("@ref")).WithType(RefType(ParseTypeName("__Native"))))))
                .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.UnsafeKeyword)))
                .WithBody(
                    Block(
                        csStruct.Fields
                            .Where(field => (field.Relations?.Count ?? 0) == 0)
                            .Select(field =>
                                generators.Marshalling.GetMarshaller(field).GenerateNativeToManaged(field, false))
                            .Where(statement => statement != null)
                    )
                );
        }
    }
}
