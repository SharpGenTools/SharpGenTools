using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal class InterfaceMarshaller : MarshallerBase, IMarshaller
    {
        public InterfaceMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public bool CanMarshal(CsMarshalBase csElement) => csElement.IsInterface && !csElement.IsArray;

        public ArgumentSyntax GenerateManagedArgument(CsParameter csElement)
        {
            var arg = Argument(IdentifierName(csElement.Name));

            if (csElement.IsOut && !csElement.IsFastOut)
            {
                return arg.WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword));
            }
            else if (csElement.IsRef || csElement.IsRefIn)
            {
                return arg.WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword));
            }

            return arg;
        }

        public ParameterSyntax GenerateManagedParameter(CsParameter csElement)
        {
            var param = Parameter(Identifier(csElement.Name));

            if (csElement.IsFastOut)
            {
                var iface = (CsInterface)csElement.PublicType;
                param = param.WithType(ParseTypeName(iface.GetNativeImplementationOrThis().QualifiedName));
            }
            else
            {
                param = param.WithType(ParseTypeName(csElement.PublicType.QualifiedName));

                if (csElement.IsOut)
                {
                    param = param.AddModifiers(Token(SyntaxKind.OutKeyword));
                }
                else if (csElement.IsRef || csElement.IsRefIn)
                {
                    param = param.AddModifiers(Token(SyntaxKind.RefKeyword));
                }
            }

            return param;
        }

        public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame) =>
            MarshalInterfaceInstanceToNative(
                csElement,
                IdentifierName(csElement.Name),
                GetMarshalStorageLocation(csElement)
            );

        public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement)
        {
            yield return LocalDeclarationStatement(
                VariableDeclaration(
                    IntPtrType,
                    SingletonSeparatedList(
                        VariableDeclarator(
                            GetMarshalStorageLocationIdentifier(csElement),
                            default,
                            EqualsValueClause(IntPtrZero)
                        )
                    )
                )
            );
        }

        public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement) => Argument(
            csElement.IsOut
                ? PrefixUnaryExpression(SyntaxKind.AddressOfExpression, GetMarshalStorageLocation(csElement))
                : CastExpression(VoidPtrType, GetMarshalStorageLocation(csElement))
        );

        public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame) =>
            GenerateGCKeepAlive(csElement);

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame) =>
            MarshalInterfaceInstanceFromNative(
                csElement,
                IdentifierName(csElement.Name),
                GetMarshalStorageLocation(csElement)
            );

        public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement) =>
            Enumerable.Empty<StatementSyntax>();

        public FixedStatementSyntax GeneratePin(CsParameter csElement) => null;

        public bool GeneratesMarshalVariable(CsMarshalCallableBase csElement) => true;

        public TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement) => IntPtrType;
    }
}
