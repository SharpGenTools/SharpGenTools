using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    class InterfaceMarshaller : MarshallerBase, IMarshaller
    {
        public InterfaceMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public bool CanMarshal(CsMarshalBase csElement)
        {
            return csElement.IsInterface && !csElement.IsArray;
        }

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

        public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame)
        {
            return MarshalInterfaceInstanceToNative(
               csElement,
               IdentifierName(csElement.Name),
               GetMarshalStorageLocation(csElement));
        }

        public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement)
        {
            yield return LocalDeclarationStatement(
               VariableDeclaration(
                   QualifiedName(
                       IdentifierName("System"),
                       IdentifierName("IntPtr")),
                   SingletonSeparatedList(
                       VariableDeclarator(GetMarshalStorageLocationIdentifier(csElement))
                           .WithInitializer(
                               EqualsValueClause(
                                   MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                       MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                           IdentifierName("System"),
                                           IdentifierName("IntPtr")),
                                       IdentifierName("Zero")))))));
        }

        public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement)
        {
            if (csElement.IsOut)
            {
                return Argument(PrefixUnaryExpression(SyntaxKind.AddressOfExpression, GetMarshalStorageLocation(csElement)));
            }
            return Argument(CastExpression(
                PointerType(
                    PredefinedType(
                        Token(SyntaxKind.VoidKeyword))),
                GetMarshalStorageLocation(csElement)));
        }

        public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame)
        {
            return GenerateGCKeepAlive(csElement);
        }

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame)
        {
            return MarshalInterfaceInstanceFromNative(
                csElement,
                IdentifierName(csElement.Name),
                GetMarshalStorageLocation(csElement));
        }

        public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement)
        {
            return Enumerable.Empty<StatementSyntax>();
        }

        public FixedStatementSyntax GeneratePin(CsParameter csElement)
        {
            return null;
        }

        public bool GeneratesMarshalVariable(CsMarshalCallableBase csElement)
        {
            return true;
        }

        public TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement)
        {
            return IntPtrType;
        }
    }
}
