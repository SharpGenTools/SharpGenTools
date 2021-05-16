using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal sealed class InterfaceMarshaller : MarshallerBase, IMarshaller
    {
        public bool CanMarshal(CsMarshalBase csElement) => csElement.IsInterface && !csElement.IsArray;

        public ArgumentSyntax GenerateManagedArgument(CsParameter csElement)
        {
            var arg = Argument(IdentifierName(csElement.Name));

            if (csElement.IsOut && !csElement.IsFast)
                return arg.WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword));

            if (csElement.IsRef || csElement.IsRefIn)
                return arg.WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword));

            return arg;
        }

        public ParameterSyntax GenerateManagedParameter(CsParameter csElement)
        {
            var param = Parameter(Identifier(csElement.Name));
            TypeSyntax type;

            if (csElement.IsOut && csElement.IsFast)
            {
                type = ParseTypeName(csElement.PublicType.GetNativeImplementationQualifiedName());
            }
            else
            {
                type = ParseTypeName(csElement.PublicType.QualifiedName);

                if (csElement.IsOut)
                {
                    param = param.AddModifiers(Token(SyntaxKind.OutKeyword));
                }
                else if (csElement.IsRef || csElement.IsRefIn)
                {
                    param = param.AddModifiers(Token(SyntaxKind.RefKeyword));
                }
            }

            return param.WithType(type);
        }

        public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame) =>
            ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    GetMarshalStorageLocation(csElement),
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            GlobalNamespace.GetTypeNameSyntax(WellKnownName.MarshallingHelpers),
                            GenericName(Identifier("ToCallbackPtr"))
                               .WithTypeArgumentList(
                                    TypeArgumentList(
                                        SingletonSeparatedList<TypeSyntax>(
                                            IdentifierName(csElement.PublicType.QualifiedName)
                                        )
                                    )
                                )
                        ),
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(IdentifierName(csElement.Name))
                            )
                        )
                    )
                )
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
                : GeneratorHelpers.CastExpression(VoidPtrType, GetMarshalStorageLocation(csElement))
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

        public InterfaceMarshaller(Ioc ioc) : base(ioc)
        {
        }
    }
}
