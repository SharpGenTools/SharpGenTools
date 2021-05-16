using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal sealed class ArrayOfInterfaceMarshaller : ArrayMarshallerBase
    {
        public override bool CanMarshal(CsMarshalBase csElement) => csElement.IsArray && csElement.IsInterface;

        public override StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame) =>
            GenerateNullCheckIfNeeded(
                csElement,
                ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            GlobalNamespace.GetTypeNameSyntax(WellKnownName.MarshallingHelpers),
                            GenericName(Identifier("ConvertToPointerArray"))
                               .WithTypeArgumentList(
                                    TypeArgumentList(
                                        SingletonSeparatedList<TypeSyntax>(
                                            IdentifierName(csElement.PublicType.QualifiedName)
                                        )
                                    )
                                )
                        ),
                        ArgumentList(
                            SeparatedList(
                                new[]
                                {
                                    // Span<IntPtr> pointers, ReadOnlySpan<TCallback> interfaces
                                    Argument(GetMarshalStorageLocation(csElement)),
                                    Argument(IdentifierName(csElement.Name))
                                }
                            )
                        )
                    )
                )
            );

        public override StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame) =>
            GenerateGCKeepAlive(csElement);

        public override StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame) =>
            csElement switch
            {
                CsParameter {IsFast: true, IsOut: true} => GenerateNullCheckIfNeeded(
                    csElement,
                    ExpressionStatement(
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                GlobalNamespace.GetTypeNameSyntax(WellKnownName.MarshallingHelpers),
                                GenericName(Identifier("ConvertToInterfaceArrayFast"))
                                   .WithTypeArgumentList(
                                        TypeArgumentList(
                                            SingletonSeparatedList<TypeSyntax>(
                                                IdentifierName(csElement.PublicType.QualifiedName)
                                            )
                                        )
                                    )
                            ),
                            ArgumentList(
                                SeparatedList(new[]
                                    {
                                        // ReadOnlySpan<IntPtr> pointers, Span<TCallback> interfaces
                                        Argument(GetMarshalStorageLocation(csElement)),
                                        Argument(IdentifierName(csElement.Name))
                                    }
                                )
                            )
                        )
                    )
                ),
                _ => LoopThroughArrayParameter(
                    csElement,
                    (publicElement, marshalElement) =>
                        MarshalInterfaceInstanceFromNative(csElement, publicElement, marshalElement)
                )
            };

        protected override TypeSyntax GetMarshalElementTypeSyntax(CsMarshalBase csElement) => IntPtrType;

        public ArrayOfInterfaceMarshaller(Ioc ioc) : base(ioc)
        {
        }
    }
}
