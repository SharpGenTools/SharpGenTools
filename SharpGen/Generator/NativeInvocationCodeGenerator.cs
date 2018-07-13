using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SharpGen.Generator
{
    class NativeInvocationCodeGenerator : MarshallingCodeGeneratorBase, ICodeGenerator<CsCallable, ExpressionSyntax>
    {
        public NativeInvocationCodeGenerator(IGeneratorRegistry generators, GlobalNamespaceProvider globalNamespace)
            :base(globalNamespace)
        {
            Generators = generators;
            this.globalNamespace = globalNamespace;
        }

        readonly GlobalNamespaceProvider globalNamespace;

        public IGeneratorRegistry Generators { get; }
        
        public ExpressionSyntax GenerateCode(CsCallable callable)
        {
            var arguments = new List<ArgumentSyntax>();

            if (callable is CsMethod)
            {
                arguments.Add(Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            ThisExpression(),
                                            IdentifierName("_nativePointer"))));
            }

            if (callable.IsReturnStructLarge)
            {
                arguments.Add(Generators.Marshalling.GetMarshaller(callable.ReturnValue).GenerateNativeArgument(callable.ReturnValue)); 
            }

            arguments.AddRange(callable.Parameters.Select(param => Generators.Marshalling.GetMarshaller(param).GenerateNativeArgument(param)));

            if (callable is CsMethod method)
            {
                arguments.Add(Argument(
                    ElementAccessExpression(
                        ParenthesizedExpression(
                            PrefixUnaryExpression(SyntaxKind.PointerIndirectionExpression,
                                CastExpression(PointerType(PointerType(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))))),
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        ThisExpression(),
                                        IdentifierName("_nativePointer"))))),
                        BracketedArgumentList(
                            SingletonSeparatedList(
                                Argument(method.CustomVtbl ?
                                (ExpressionSyntax)MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    ThisExpression(),
                                    IdentifierName($"{callable.Name}__vtbl_index"))
                                : LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(method.Offset))
                                )
                            )))));
            }

            var call = InvocationExpression(
                    IdentifierName(callable is CsFunction ?
                        callable.CppElementName + "_"
                    : callable.GetParent<CsAssembly>().QualifiedName + ".LocalInterop." + callable.Interop.Name),
                    ArgumentList(SeparatedList(arguments)));

            return callable.IsReturnStructLarge || !callable.HasReturnType ?
                (ExpressionSyntax)call
                : AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    NeedsMarshalling(callable.ReturnValue) ?
                        GetMarshalStorageLocation(callable.ReturnValue)
                        : IdentifierName(callable.ReturnValue.Name),
                    call
                    );
        }

    }
}
