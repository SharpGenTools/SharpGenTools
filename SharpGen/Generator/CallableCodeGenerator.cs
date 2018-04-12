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

            // method signature (commented if hidden)
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

            var statements = new List<StatementSyntax>();

            string resultVariableName = null;
            var resultMarshallingRequired = false;
            
            statements.AddRange(csElement.Parameters.SelectMany(param => Generators.ParameterProlog.GenerateCode(param)));

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

            if (csElement.HasReturnType)
            {
                resultVariableName = csElement.ReturnValue.Name;
                statements.Add(LocalDeclarationStatement(
                    VariableDeclaration(
                        ParseTypeName(csElement.ReturnValue.PublicType.QualifiedName),
                        SingletonSeparatedList(
                            VariableDeclarator(resultVariableName)))));
                if (csElement.ReturnValue.PublicType is CsStruct returnStruct && returnStruct.HasMarshalType)
                {
                    resultMarshallingRequired = true;
                    resultVariableName = csElement.ReturnValue.MarshalStorageLocation;
                    statements.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(csElement.ReturnValue.Name),
                        ObjectCreationExpression(ParseTypeName(csElement.ReturnValue.PublicType.QualifiedName))
                            .WithArgumentList(ArgumentList()))));
                    statements.Add(LocalDeclarationStatement(
                        VariableDeclaration(
                            ParseTypeName(csElement.ReturnValue.PublicType.QualifiedName + ".__Native"),
                            SingletonSeparatedList(
                                VariableDeclarator(resultVariableName)))));
                }
            }

            var fixedStatements = csElement.PublicParameters
                .Select(Generators.Pinning.GenerateCode)
                .Where(stmt => stmt != null).ToList();

            var invocation = Generators.NativeInvocation.GenerateCode(csElement);
            var callStmt = ExpressionStatement(csElement.HasReturnType && !csElement.IsReturnStructLarge ?
                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(resultVariableName),
                    invocation)
                    : invocation);

            var fixedStatement = fixedStatements.FirstOrDefault()?.WithStatement(callStmt);
            foreach (var statement in fixedStatements.Skip(1))
            {
                fixedStatement = statement.WithStatement(fixedStatement);
            }

            statements.Add((StatementSyntax)fixedStatement ?? callStmt);

            if (resultMarshallingRequired)
            {
                statements.Add(
                    ExpressionStatement(
                        InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(csElement.ReturnValue.Name),
                                IdentifierName("__MarshalFrom")),
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(IdentifierName(csElement.ReturnValue.MarshalStorageLocation))
                                        .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)))))));
            }

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
