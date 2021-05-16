using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal sealed class RefWrapperMarshaller : WrapperMarshallerBase
    {
        public RefWrapperMarshaller(Ioc ioc, IMarshaller implementation) : base(ioc, implementation)
        {
        }

        public static bool IsApplicable(CsMarshalBase csElement) =>
            csElement is CsParameter {IsLocalManagedReference: true, IsArray: false, IsOptional: true};

        public override StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame) =>
            GenerateManagedToNativeForOptional(
                (CsParameter) csElement,
                base.GenerateManagedToNative(csElement, singleStackFrame)
            );

        private StatementSyntax GenerateManagedToNativeForOptional(CsParameter parameter, StatementSyntax statement)
        {
            var refIdentifier = IdentifierName(GetRefLocationIdentifier(parameter));

            StatementSyntaxList statements = new()
            {
                statement,
                IfStatement(
                    BinaryExpression(
                        SyntaxKind.NotEqualsExpression,
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                GlobalNamespace.GetTypeNameSyntax(BuiltinType.Unsafe),
                                IdentifierName(nameof(Unsafe.AsPointer))
                            ),
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(refIdentifier).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword))
                                )
                            )
                        ),
                        DefaultLiteral
                    ),
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression, refIdentifier,
                            GeneratesMarshalVariable(parameter)
                                ? GetMarshalStorageLocation(parameter)
                                : IdentifierName(parameter.Name)
                        )
                    )
                )
            };

            return statements.ToStatement();
        }
    }
}