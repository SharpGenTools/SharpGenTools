using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Microsoft.CodeAnalysis;
using SharpGen.Transform;

namespace SharpGen.Generator
{
    class CallableCodeGenerator : MemberCodeGeneratorBase<CsCallable>
    {
        public CallableCodeGenerator(IGeneratorRegistry generators, IDocumentationLinker documentation, ExternalDocCommentsReader docReader, GlobalNamespaceProvider globalNamespace)
            :base(documentation, docReader)
        {
            Generators = generators;
            this.globalNamespace = globalNamespace;
        }

        readonly GlobalNamespaceProvider globalNamespace;

        public IGeneratorRegistry Generators { get; }

        public override IEnumerable<MemberDeclarationSyntax> GenerateCode(CsCallable csElement)
        {
            // Documentation
            var documentationTrivia = GenerateDocumentationTrivia(csElement);

            // method signature
            var methodDeclaration = MethodDeclaration(ParseTypeName(csElement.PublicReturnTypeQualifiedName), csElement.Name)
                .WithModifiers(TokenList(ParseTokens(csElement.VisibilityName)).Add(Token(SyntaxKind.UnsafeKeyword)))
                .WithParameterList(
                    ParameterList(
                        SeparatedList(
                            csElement.PublicParameters.Select(param =>
                                Generators.Parameter.GenerateCode(param)
                                    .WithDefault(param.DefaultValue == null ? default
                                        : EqualsValueClause(ParseExpression(param.DefaultValue)))
                                )
                            )
                        )
                )
                .WithLeadingTrivia(Trivia(documentationTrivia));

            if (csElement.SignatureOnly)
            {
                yield return methodDeclaration
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                    .WithModifiers(TokenList());
                yield break;
            }

            var statements = new List<StatementSyntax>();
            
            statements.AddRange(csElement.Parameters.SelectMany(param => Generators.CallableProlog.GenerateCode(param)));
            if (csElement.HasReturnType)
            {
                statements.AddRange(Generators.CallableProlog.GenerateCode(csElement.ReturnValue)); 
            }


            foreach (var param in csElement.Parameters)
            {
                if (param.IsIn || param.IsRefIn || param.IsRef)
                {
                    var marshalToNative = Generators.MarshalToNativeSingleFrame.GenerateCode(param);
                    if (marshalToNative != null)
                    {
                        statements.Add(marshalToNative);
                    }
                }
            }

            var fixedStatements = csElement.PublicParameters
                .Select(Generators.Pinning.GenerateCode)
                .Where(stmt => stmt != null).ToList();

            var callStmt = ExpressionStatement(Generators.NativeInvocation.GenerateCode(csElement));

            var fixedStatement = fixedStatements.FirstOrDefault()?.WithStatement(callStmt);
            foreach (var statement in fixedStatements.Skip(1))
            {
                fixedStatement = statement.WithStatement(fixedStatement);
            }

            statements.Add((StatementSyntax)fixedStatement ?? callStmt);

            foreach (var param in csElement.Parameters)
            {
                if (param.IsRef || param.IsOut)
                {
                    var marshalFromNative = Generators.MarshalFromNativeSingleFrame.GenerateCode(param);
                    if (marshalFromNative != null)
                    {
                        statements.Add(marshalFromNative);
                    }
                }
            }

            if (csElement.HasReturnType)
            {
                var marshalReturnType = Generators.MarshalFromNativeSingleFrame.GenerateCode(csElement.ReturnValue);
                if (marshalReturnType != null)
                {
                    statements.Add(marshalReturnType);
                }
            }
            
            statements.AddRange(csElement.Parameters
                .Where(param => !param.IsOut)
                .Select(Generators.MarshalCleanupSingleFrame.GenerateCode)
                .Where(param => param != null));


            if ((csElement.ReturnValue.PublicType.Name == globalNamespace.GetTypeName(WellKnownName.Result)) && csElement.CheckReturnType)
            {
                statements.Add(ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(csElement.ReturnValue.Name),
                        IdentifierName("CheckError")))));
            }

            // Return
            if (csElement.HasPublicReturnType)
            {
                if (csElement.HasReturnTypeParameter || csElement.ForceReturnType || !csElement.HideReturnType)
                {
                    statements.Add(ReturnStatement(IdentifierName(csElement.ReturnName)));
                }
            }

            yield return methodDeclaration.WithBody(Block(statements));
        }
    }
}
