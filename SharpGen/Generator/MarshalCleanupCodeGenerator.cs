using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;

namespace SharpGen.Generator
{
    class MarshalCleanupCodeGenerator : MarshallingCodeGeneratorBase, ICodeGenerator<CsMarshalBase, StatementSyntax>
    {
        private readonly GlobalNamespaceProvider globalNamespace;
        private readonly bool singleStack;

        private bool MarshalPinnableElements => !singleStack;

        public MarshalCleanupCodeGenerator(bool singleStack, GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
            this.singleStack = singleStack;
            this.globalNamespace = globalNamespace;
        }

        public StatementSyntax GenerateCode(CsMarshalBase csElement)
        {
            if (csElement.IsString && (!csElement.IsWideChar || MarshalPinnableElements))
            {
                return ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            globalNamespace.GetTypeNameSyntax(BuiltinType.Marshal),
                            IdentifierName("FreeHGlobal")),
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(GetMarshalStorageLocation(csElement))))));
            }

            if (csElement.IsArray && csElement.HasNativeValueType)
            {
                return LoopThroughArrayParameter(
                    csElement,
                    (publicElement, marshalElement) =>
                        CreateMarshalStructStatement(
                            csElement,
                            "__MarshalFree",
                            publicElement,
                            marshalElement));
            }

            if (csElement.HasNativeValueType && !csElement.IsArray)
            {
                ExpressionSyntax publicElementExpression = IdentifierName(csElement.Name);

                if (csElement.IsNullableStruct)
                {
                    publicElementExpression = MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        publicElementExpression,
                        IdentifierName("Value"));
                }

                return CreateMarshalStructStatement(
                        csElement,
                        "__MarshalFree",
                        publicElementExpression,
                        GetMarshalStorageLocation(csElement)
                );
            }

            return null;
        }
    }
}
