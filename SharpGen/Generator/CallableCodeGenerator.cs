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
using SharpGen.Logging;

namespace SharpGen.Generator
{
    class CallableCodeGenerator : MemberCodeGeneratorBase<CsCallable>
    {
        public CallableCodeGenerator(IGeneratorRegistry generators, IDocumentationLinker documentation, ExternalDocCommentsReader docReader, GlobalNamespaceProvider globalNamespace, Logger logger)
            :base(documentation, docReader)
        {
            Generators = generators;
            this.globalNamespace = globalNamespace;
            this.logger = logger;
        }

        private readonly GlobalNamespaceProvider globalNamespace;
        private readonly Logger logger;

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
                                Generators.Marshalling.GetMarshaller(param).GenerateManagedParameter(param)
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

            foreach (var param in csElement.Parameters)
            {
                if ((param.Relations?.Count ?? 0) == 0)
                {
                    if (param.UsedAsReturn)
                    {
                        statements.Add(GenerateManagedHiddenMarshallableProlog(param));
                    }
                    statements.AddRange(Generators.Marshalling.GetMarshaller(param).GenerateManagedToNativeProlog(param));
                }
                else
                {
                    statements.Add(GenerateManagedHiddenMarshallableProlog(param));

                    foreach (var relation in param.Relations)
                    {
                        if (!ValidRelationInScenario(relation))
                        {
                            logger.Error(LoggingCodes.InvalidRelationInScenario, $"The relation \"{relation}\" is invalid in a method/function.");
                            continue;
                        }

                        var marshaller = Generators.Marshalling.GetRelationMarshaller(relation);
                        StatementSyntax marshalToNative;
                        var relatedMarshallableName = (relation as LengthRelation)?.Identifier;
                        if (relatedMarshallableName is null)
                        {
                            marshalToNative = marshaller.GenerateManagedToNative(null, param);
                        }
                        else
                        {
                            var relatedParameter = csElement.Parameters.Find(p => p.CppElementName == relatedMarshallableName);

                            if (relatedParameter is null)
                            {
                                logger.Error(LoggingCodes.InvalidRelationInScenario, $"The relation with \"{relatedMarshallableName}\" parameter is invalid in a method/function \"{csElement.Name}\".");
                                continue;
                            }

                            marshalToNative = marshaller.GenerateManagedToNative(relatedParameter, param);
                        }

                        if (marshalToNative != null)
                        {
                            statements.Add(marshalToNative);
                        }
                    }

                    statements.AddRange(Generators.Marshalling.GetMarshaller(param).GenerateManagedToNativeProlog(param));
                }
            }

            if (csElement.HasReturnType)
            {
                statements.Add(GenerateManagedHiddenMarshallableProlog(csElement.ReturnValue));
                statements.AddRange(
                    Generators.Marshalling.GetMarshaller(csElement.ReturnValue)
                        .GenerateManagedToNativeProlog(csElement.ReturnValue));
            }


            foreach (var param in csElement.Parameters)
            {
                if (param.IsIn || param.IsRefIn || param.IsRef)
                {
                    var marshaller = Generators.Marshalling.GetMarshaller(param);
                    var marshalToNative = marshaller.GenerateManagedToNative(param, true);
                    if (marshalToNative != null)
                    {
                        statements.Add(marshalToNative);
                    }
                }
            }

            var fixedStatements = csElement.PublicParameters
                .Select(param => Generators.Marshalling.GetMarshaller(param).GeneratePin(param))
                .Where(stmt => stmt != null).ToList();

            var callStmt = GeneratorHelpers.GetPlatformSpecificStatements(
                    globalNamespace,
                    Generators.Config,
                    csElement.InteropSignatures.Keys,
                    (platform) => 
                        ExpressionStatement(
                            Generators.NativeInvocation.GenerateCode((csElement, platform, csElement.InteropSignatures[platform]))));

            var fixedStatement = fixedStatements.FirstOrDefault()?.WithStatement(callStmt);
            foreach (var statement in fixedStatements.Skip(1))
            {
                fixedStatement = statement.WithStatement(fixedStatement);
            }

            statements.Add(fixedStatement ?? callStmt);

            foreach (var param in csElement.Parameters)
            {
                if (param.IsRef || param.IsOut)
                {
                    var marshaller = Generators.Marshalling.GetMarshaller(param);
                    var marshalFromNative = marshaller.GenerateNativeToManaged(param, true);
                    if (marshalFromNative != null)
                    {
                        statements.Add(marshalFromNative);
                    }
                }
            }

            if (csElement.HasReturnType)
            {
                var marshaller = Generators.Marshalling.GetMarshaller(csElement.ReturnValue);
                var marshalReturnType = marshaller.GenerateNativeToManaged(csElement.ReturnValue, true);
                if (marshalReturnType != null)
                {
                    statements.Add(marshalReturnType);
                }
            }
            
            statements.AddRange(csElement.Parameters
                .Where(param => !param.IsOut)
                .Select(param => Generators.Marshalling.GetMarshaller(param).GenerateNativeCleanup(param, true))
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

        private StatementSyntax GenerateManagedHiddenMarshallableProlog(CsMarshalCallableBase csElement)
        {
            var type = csElement.IsArray ?
                ArrayType(
                    ParseTypeName(csElement.PublicType.QualifiedName),
                    SingletonList(ArrayRankSpecifier()))
                : ParseTypeName(csElement.PublicType.QualifiedName);

            return LocalDeclarationStatement(
                VariableDeclaration(
                    type,
                    SingletonSeparatedList(
                        VariableDeclarator(csElement.Name))));
        }

        private static bool ValidRelationInScenario(MarshallableRelation relation)
        {
            return relation is ConstantValueRelation || relation is LengthRelation;
        }
    }
}
