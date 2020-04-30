using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;

namespace SharpGen.Generator
{
    class NativeStructCodeGenerator : IMultiCodeGenerator<CsStruct, MemberDeclarationSyntax>
    {
        public NativeStructCodeGenerator(IGeneratorRegistry generators, GlobalNamespaceProvider globalNamespace)
        {
            Generators = generators;
            this.globalNamespace = globalNamespace;
        }

        readonly GlobalNamespaceProvider globalNamespace;

        public IGeneratorRegistry Generators { get; }

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
                .WithMembers(List(csStruct.Fields.SelectMany(csField => GenerateMarshalStructField(csStruct, csField))));

            yield return marshalStruct;

            yield return GenerateMarshalFree(csStruct);

            yield return GenerateMarshalFrom(csStruct);

            yield return GenerateMarshalTo(csStruct);
        }


        private IEnumerable<MemberDeclarationSyntax> GenerateMarshalStructField(CsStruct csStruct, CsField field)
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
                        var offset = field.Offset + (field.Size / field.ArrayDimensionValue) * i;
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

        private MethodDeclarationSyntax GenerateMarshalFree(CsStruct csStruct)
            => MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), "__MarshalFree")
                .WithParameterList(ParameterList(SingletonSeparatedList(
                    Parameter(Identifier("@ref")).WithType(RefType(ParseTypeName("__Native")))))).WithBody(
                Block(
                    List(csStruct.Fields
                        .Where(field => !field.IsArray)
                    .Select(field => Generators.Marshalling.GetMarshaller(field).GenerateNativeCleanup(field, false))
                        .Where(statement => statement != null))))
            .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.UnsafeKeyword)));


        private MethodDeclarationSyntax GenerateMarshalTo(CsStruct csStruct)
        {
            IEnumerable<StatementSyntax> FieldMarshallers(CsField field)
            {
                if ((field.Relations?.Count ?? 0) == 0)
                {
                    yield return Generators.Marshalling.GetMarshaller(field).GenerateManagedToNative(field, false);
                    yield break;
                }

                foreach (var relation in field.Relations)
                {
                    var marshaller = Generators.Marshalling.GetRelationMarshaller(relation);
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
                                Generators.Marshalling.GetMarshaller(field).GenerateNativeToManaged(field, false))
                            .Where(statement => statement != null)
                    )
                );
        }
    }
}
