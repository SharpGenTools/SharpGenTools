using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    class ValueTypeMarshaller : MarshallerBase, IMarshaller
    {
        public ValueTypeMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public bool CanMarshal(CsMarshalBase csElement)
        {
            return csElement.IsValueType && !csElement.IsArray && !csElement.MappedToDifferentPublicType && !(csElement is CsField);
        }

        public ArgumentSyntax GenerateManagedArgument(CsParameter csElement)
        {
            return GenerateManagedValueTypeArgument(csElement);
        }

        public ParameterSyntax GenerateManagedParameter(CsParameter csElement)
        {
            return GenerateManagedValueTypeParameter(csElement);
        }

        public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame)
        {
            return null;
        }

        public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement)
        {
            if (csElement.IsOut && !csElement.IsPrimitive && !csElement.UsedAsReturn)
            {
                yield return ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(csElement.Name),
                        DefaultExpression(ParseTypeName(csElement.PublicType.QualifiedName))
                ));
            }
        }

        public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement)
        {
            if (csElement.PassedByNativeReference)
            {
                if (csElement.IsFixed && !csElement.UsedAsReturn)
                {
                    return Argument(GetMarshalStorageLocation(csElement));
                }
                else
                {
                    return Argument(PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                IdentifierName(csElement.Name)));
                }
            }
            else
            {
                return Argument(IdentifierName(csElement.Name));
            }
        }

        public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame)
        {
            return null;
        }

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame)
        {
            return null;
        }

        public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement)
        {
            return Enumerable.Empty<StatementSyntax>();
        }

        public FixedStatementSyntax GeneratePin(CsParameter csElement)
        {
            if (csElement.IsFixed && !csElement.IsUsedAsReturnType)
            {
                return FixedStatement(VariableDeclaration(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                    SingletonSeparatedList(
                        VariableDeclarator(GetMarshalStorageLocationIdentifier(csElement)).WithInitializer(EqualsValueClause(
                            PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                IdentifierName(csElement.Name))
                            )))), EmptyStatement());
            }
            return null;
        }

        public bool GeneratesMarshalVariable(CsMarshalCallableBase csElement)
        {
            return false;
        }

        public TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement)
        {
            return ParseTypeName(csElement.PublicType.QualifiedName);
        }
    }
}
