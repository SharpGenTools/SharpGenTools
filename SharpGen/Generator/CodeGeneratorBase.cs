using System;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Generator.Marshallers;
using SharpGen.Logging;
using SharpGen.Model;
using SharpGen.Transform;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    public abstract class CodeGeneratorBase
    {
        private static readonly NameSyntax FieldOffsetName = ParseName("System.Runtime.InteropServices.FieldOffset");

        protected GlobalNamespaceProvider GlobalNamespace => globalNamespace ??= ioc.GlobalNamespace;
        protected Logger Logger => logger ??= ioc.Logger;
        protected IGeneratorRegistry Generators => generators ??= ioc.Generators;
        protected IDocumentationLinker DocumentationLinker => documentationLinker ??= ioc.DocumentationLinker;
        protected ExternalDocCommentsReader ExternalDocReader => externalDocReader ??= ioc.ExternalDocReader;

        private GlobalNamespaceProvider globalNamespace;
        private Logger logger;
        private IGeneratorRegistry generators;
        private IDocumentationLinker documentationLinker;
        private ExternalDocCommentsReader externalDocReader;
        private readonly Ioc ioc;

        protected CodeGeneratorBase(Ioc ioc)
        {
            this.ioc = ioc ?? throw new ArgumentNullException(nameof(ioc));
        }

        protected IMarshaller GetMarshaller(CsMarshalBase csElement) => Generators.Marshalling.GetMarshaller(csElement);

        protected IRelationMarshaller GetRelationMarshaller(MarshallableRelation relation) =>
            Generators.Marshalling.GetRelationMarshaller(relation);

        private static AttributeSyntax FieldOffsetAttribute(SyntaxToken value) => Attribute(
            FieldOffsetName,
            AttributeArgumentList(
                SingletonSeparatedList(
                    AttributeArgument(LiteralExpression(SyntaxKind.NumericLiteralExpression, value))
                )
            )
        );

        private static T AddFieldOffsetAttribute<T>(T member, int value, bool replace) where T : MemberDeclarationSyntax
        {
            if (replace)
                RemoveAttribute(ref member, FieldOffsetName);

            return (T) member.AddAttributeLists(
                AttributeList(SingletonSeparatedList(FieldOffsetAttribute(Literal(value))))
            );
        }

        private static void RemoveAttribute<T>(ref T member, NameSyntax attributeName) where T : MemberDeclarationSyntax
        {
            int outerIndex;
            while ((outerIndex = AttributeListSearch(member)) != -1)
            {
                var oldAttributeList = member.AttributeLists[outerIndex];
                var attributeList = oldAttributeList.Attributes;

                int innerIndex;
                while ((innerIndex = AttributeSearch(attributeList)) != -1)
                {
                    attributeList = attributeList.RemoveAt(innerIndex);
                }

                var newAttributeList = attributeList.Count == 0
                                           ? member.AttributeLists.RemoveAt(outerIndex)
                                           : member.AttributeLists.Replace(
                                               oldAttributeList,
                                               oldAttributeList.WithAttributes(attributeList)
                                           );

                member = (T) member.WithAttributeLists(newAttributeList);
            }

            int AttributeListSearch(T memberDeclarationSyntax) =>
                memberDeclarationSyntax.AttributeLists.IndexOf(x => AttributeSearch(x.Attributes) != -1);

            int AttributeSearch(SeparatedSyntaxList<AttributeSyntax> attributeListSyntax) =>
                attributeListSyntax.IndexOf(x => x.Name.IsEquivalentTo(attributeName));
        }

        protected static T AddFieldOffsetAttribute<T>(T member, uint value, bool replace = false)
            where T : MemberDeclarationSyntax =>
            AddFieldOffsetAttribute(member, checked((int) value), replace);

        protected static T AddFieldOffsetAttribute<T>(T member, long value, bool replace = false)
            where T : MemberDeclarationSyntax =>
            AddFieldOffsetAttribute(member, checked((int) value), replace);
    }
}