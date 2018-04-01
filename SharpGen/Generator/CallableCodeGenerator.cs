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

        GlobalNamespaceProvider globalNamespace;

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

            // foreach parameter
            foreach (var parameter in csElement.Parameters)
            {
                statements.AddRange(Generators.ParameterProlog.GenerateCode(parameter));
            }
            if (csElement.HasReturnType)
            {
                resultVariableName = "__result__";
                statements.Add(LocalDeclarationStatement(
                    VariableDeclaration(
                        ParseTypeName(csElement.ReturnValue.PublicType.QualifiedName),
                        SingletonSeparatedList(
                            VariableDeclarator(resultVariableName)))));
                if (csElement.ReturnValue.PublicType is CsStruct returnStruct && returnStruct.HasMarshalType)
                {
                    resultMarshallingRequired = true;
                    resultVariableName = "__result__native";
                    statements.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName("__result__"),
                        ObjectCreationExpression(ParseTypeName(csElement.ReturnValue.PublicType.QualifiedName))
                            .WithArgumentList(ArgumentList()))));
                    statements.Add(LocalDeclarationStatement(
                        VariableDeclaration(
                            ParseTypeName(csElement.ReturnValue.PublicType.QualifiedName + ".__Native"),
                            SingletonSeparatedList(
                                VariableDeclarator(resultVariableName)))));
                }
            }

            var fixedStatements = GenerateFixedStatements(csElement);

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
                                IdentifierName("__result__"),
                                IdentifierName("__MarshalFrom")),
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(IdentifierName("__result__native"))
                                        .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)))))));
            }

            foreach (var parameter in csElement.Parameters)
            {
                statements.AddRange(Generators.ParameterEpilog.GenerateCode(parameter));
            }

            // Return
            if (csElement.HasPublicReturnType)
            {
                if ((csElement.ReturnValue.PublicType.Name == globalNamespace.GetTypeName(WellKnownName.Result)) && csElement.CheckReturnType)
                {
                    statements.Add(ExpressionStatement(
                        InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("__result__"),
                            IdentifierName("CheckError")))));
                }

                if (csElement.HasReturnTypeParameter || csElement.ForceReturnType || !csElement.HideReturnType)
                {
                    statements.Add(ReturnStatement(IdentifierName(csElement.ReturnName)));
                }
            }

            yield return methodDeclaration.WithBody(Block(statements));
        }


        private static List<FixedStatementSyntax> GenerateFixedStatements(CsCallable csElement)
        {
            var fixedStatements = new List<FixedStatementSyntax>();
            foreach (var param in csElement.Parameters)
            {
                FixedStatementSyntax statement = null;
                if (param.IsArray && param.IsValueType)
                {
                    if (param.HasNativeValueType || param.IsOptional)
                    {
                        statement = FixedStatement(VariableDeclaration(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                            SingletonSeparatedList(
                                VariableDeclarator(param.TempName).WithInitializer(EqualsValueClause(
                                    IdentifierName($"{param.TempName}_")
                                    )))), EmptyStatement());
                    }
                    else
                    {
                        statement = FixedStatement(VariableDeclaration(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                            SingletonSeparatedList(
                                VariableDeclarator(param.TempName).WithInitializer(EqualsValueClause(
                                    IdentifierName(param.Name)
                                    )))), EmptyStatement());
                    }
                }
                else if (param.IsFixed && param.IsValueType && !param.HasNativeValueType && !param.IsUsedAsReturnType)
                {
                    statement = FixedStatement(VariableDeclaration(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                        SingletonSeparatedList(
                            VariableDeclarator(param.TempName).WithInitializer(EqualsValueClause(
                                PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                    IdentifierName(param.Name))
                                )))), EmptyStatement());
                }
                else if (param.IsString && param.IsWideChar)
                {
                    statement = FixedStatement(VariableDeclaration(PointerType(PredefinedType(Token(SyntaxKind.CharKeyword))),
                        SingletonSeparatedList(
                            VariableDeclarator(param.TempName).WithInitializer(EqualsValueClause(
                                IdentifierName(param.Name)
                                )))), EmptyStatement());
                }

                if (statement != null)
                {
                    fixedStatements.Add(statement);
                }
            }

            return fixedStatements;
        }


    }
}
