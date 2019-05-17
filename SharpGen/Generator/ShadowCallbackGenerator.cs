using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;

namespace SharpGen.Generator
{
    class ShadowCallbackGenerator : IMultiCodeGenerator<CsCallable, MemberDeclarationSyntax>
    {
        private readonly GlobalNamespaceProvider globalNamespace;
        private readonly IGeneratorRegistry generators;

        public ShadowCallbackGenerator(IGeneratorRegistry generators, GlobalNamespaceProvider globalNamespace)
        {
            this.generators = generators;
            this.globalNamespace = globalNamespace;
        }

        public IEnumerable<MemberDeclarationSyntax> GenerateCode(CsCallable csElement)
        {
            foreach (var sig in csElement.InteropSignatures.Where(sig => (sig.Key & generators.Config.Platforms) != 0))
            {
                yield return GenerateDelegateDeclaration(csElement, sig.Key, sig.Value);
                yield return GenerateShadowCallback(csElement, sig.Key, sig.Value);
            }
        }

        private DelegateDeclarationSyntax GenerateDelegateDeclaration(CsCallable csElement, PlatformDetectionType platform, InteropMethodSignature sig)
        {
            return DelegateDeclaration(ParseTypeName(sig.ReturnType.TypeName), $"{csElement.Name}Delegate{GeneratorHelpers.GetPlatformSpecificSuffix(platform)}")
                .AddAttributeLists(
                    AttributeList(
                        SingletonSeparatedList(
                            Attribute(
                                ParseName("System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute"))
                            .AddArgumentListArguments(
                                AttributeArgument(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName("System"),
                                                    IdentifierName("Runtime")),
                                                IdentifierName("InteropServices")),
                                            IdentifierName("CallingConvention")),
                                        IdentifierName(sig.CallingConvention)))))))
            .WithParameterList(
                ParameterList(
                    (csElement is CsMethod ?
                        SingletonSeparatedList(
                            Parameter(Identifier("thisObject"))
                            .WithType(ParseTypeName("System.IntPtr")))
                        : default)
                    .AddRange(
                        sig.ParameterTypes
                            .Select((type, i) =>
                                Parameter(Identifier($"arg{i}"))
                                .WithType(ParseTypeName(type.TypeName)))
                        )))
            .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)));
        }

        private MethodDeclarationSyntax GenerateShadowCallback(CsCallable csElement, PlatformDetectionType platform, InteropMethodSignature sig)
        {
            var methodDecl = MethodDeclaration(
                ParseTypeName(sig.ReturnType.TypeName),
                $"{csElement.Name}{GeneratorHelpers.GetPlatformSpecificSuffix(platform)}")
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PrivateKeyword),
                        Token(SyntaxKind.StaticKeyword),
                        Token(SyntaxKind.UnsafeKeyword)));

            if (csElement is CsMethod)
            {
                methodDecl = methodDecl
                    .AddParameterListParameters(
                        Parameter(Identifier("thisObject"))
                        .WithType(ParseTypeName("System.IntPtr")));
            }

            bool isForcedReturnBufferSig = (sig.Flags & InteropMethodSignatureFlags.ForcedReturnBufferSig) != 0;
            IEnumerable<InteropType> nativeParameters = sig.ParameterTypes;

            if (isForcedReturnBufferSig)
            {
                nativeParameters = nativeParameters.Skip(1);
                methodDecl = methodDecl
                    .AddParameterListParameters(
                        Parameter(Identifier("returnSlot"))
                        .WithType(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword)))));
            }

            methodDecl = methodDecl
                .AddParameterListParameters(
                    nativeParameters
                            .Select((type, i) =>
                                Parameter(Identifier($"param{i}"))
                                .WithType(ParseTypeName(type.TypeName)))
                            .ToArray());

            var statements = new List<StatementSyntax>();

            statements.AddRange(generators.ReverseCallableProlog.GenerateCode((csElement, sig)));

            statements.AddRange(
                generators.Marshalling.GetMarshaller(csElement.ReturnValue).
                    GenerateNativeToManagedExtendedProlog(csElement.ReturnValue));

            foreach (var param in csElement.Parameters)
            {
                statements.AddRange(
                   generators.Marshalling.GetMarshaller(param).
                       GenerateNativeToManagedExtendedProlog(param));
            }
            
            if (csElement is CsMethod)
            {
                statements.Add(LocalDeclarationStatement(
                    VariableDeclaration(
                        IdentifierName(csElement.Parent.Name))
                    .WithVariables(
                        SingletonSeparatedList(
                            VariableDeclarator(
                                Identifier("@this"))
                            .WithInitializer(
                                EqualsValueClause(
                                    CastExpression(
                                        IdentifierName(csElement.Parent.Name),
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            InvocationExpression(
                                                GenericName(
                                                    Identifier("ToShadow"))
                                                .WithTypeArgumentList(
                                                    TypeArgumentList(
                                                        SingletonSeparatedList<TypeSyntax>(
                                                            IdentifierName(csElement.GetParent<CsInterface>().ShadowName)))))
                                            .WithArgumentList(
                                                ArgumentList(
                                                    SingletonSeparatedList(
                                                        Argument(
                                                            IdentifierName("thisObject"))))),
                                            IdentifierName("Callback")))))))));
            }

            foreach (var param in csElement.Parameters)
            {
                if (param.IsIn || param.IsRefIn || param.IsRef)
                {
                    var marshalFromNative = generators.Marshalling.GetMarshaller(param).GenerateNativeToManaged(param, false);
                    if (marshalFromNative != null)
                    {
                        statements.Add(marshalFromNative);
                    }
                }
            }
            
            var managedArguments = new List<ArgumentSyntax>();

            foreach (var param in csElement.Parameters.Where(p => !p.UsedAsReturn && p.Relation is null))
            {
                managedArguments.Add(generators.Marshalling.GetMarshaller(param).GenerateManagedArgument(param));
            }

            var callableName = csElement is CsFunction
                ? (ExpressionSyntax)IdentifierName(csElement.Name)
                : MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("@this"),
                    IdentifierName(csElement.Name));

            var invocation = InvocationExpression(callableName, ArgumentList(SeparatedList(managedArguments)));

            var returnValueMarshaller = generators.Marshalling.GetMarshaller(csElement.ReturnValue);

            var returnValueNeedsMarshalling = returnValueMarshaller.GeneratesMarshalVariable(csElement.ReturnValue);

            var hasReturnValue = csElement.HasReturnType && (!csElement.HideReturnType || csElement.ForceReturnType);

            if (!hasReturnValue && !csElement.HasReturnTypeParameter)
            {
                statements.Add(ExpressionStatement(invocation));
            }
            else
            {
                CsMarshalBase publicReturnValue;

                if (csElement.HasReturnTypeParameter)
                {
                    publicReturnValue = csElement.Parameters.First(p => p.UsedAsReturn);
                }
                else
                {
                    publicReturnValue = csElement.ReturnValue;
                }

                statements.Add(ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(publicReturnValue.Name),
                        invocation)));

                if (returnValueNeedsMarshalling && !csElement.HasReturnTypeParameter)
                {
                    statements.Add(generators.Marshalling.GetMarshaller(csElement.ReturnValue).GenerateManagedToNative(csElement.ReturnValue, false));
                }
            }

            foreach (var param in csElement.Parameters)
            {
                if (param.IsOut || param.IsRef)
                {
                    var marshalToNative = generators.Marshalling.GetMarshaller(param).GenerateManagedToNative(param, false);
                    if (marshalToNative != null)
                    {
                        statements.Add(marshalToNative);
                    }
                }
            }

            var nativeReturnLocation = returnValueNeedsMarshalling
                                ? IdentifierName(generators.Marshalling.GetMarshalStorageLocationIdentifier(csElement.ReturnValue))
                                : IdentifierName(csElement.ReturnValue.Name);

            if (csElement.HasReturnType && (!csElement.HideReturnType || csElement.ForceReturnType))
            {
                statements.Add(
                    ReturnStatement(isForcedReturnBufferSig ?
                        IdentifierName("returnSlot")
                        : nativeReturnLocation));
            }

            var exceptionVariableIdentifier = Identifier("__exception__");

            var catchClause = CatchClause()
                .WithDeclaration(
                    CatchDeclaration(ParseTypeName("System.Exception")))
                .WithBlock(Block());

            if (csElement.ReturnValue.PublicType.QualifiedName == globalNamespace.GetTypeName(WellKnownName.Result))
            {
                catchClause = catchClause
                    .WithDeclaration(catchClause.Declaration.WithIdentifier(exceptionVariableIdentifier))
                    .WithBlock(
                        Block(
                            ReturnStatement(
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            globalNamespace.GetTypeNameSyntax(WellKnownName.Result),
                                            IdentifierName("GetResultFromException")))
                                    .WithArgumentList(
                                        ArgumentList(
                                            SingletonSeparatedList(
                                                Argument(
                                                    IdentifierName(exceptionVariableIdentifier))))),
                                    IdentifierName("Code"))
                        )));

                if (csElement.HideReturnType && !csElement.ForceReturnType)
                {
                    statements.Add(ReturnStatement(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    globalNamespace.GetTypeNameSyntax(WellKnownName.Result),
                                    IdentifierName("Ok")),
                                IdentifierName("Code"))));
                }
            }
            else if (csElement.HasReturnType)
            {
                var returnStatement = isForcedReturnBufferSig ?
                    ReturnStatement(IdentifierName("returnSlot"))
                    : ReturnStatement(DefaultExpression(returnValueMarshaller.GetMarshalTypeSyntax(csElement.ReturnValue)));
                catchClause = catchClause.WithBlock(Block(returnStatement));
            }

            return methodDecl.WithBody(
                Block(
                    TryStatement()
                    .WithBlock(Block(statements))
                    .WithCatches(SingletonList(catchClause))));
        }
    }
}
