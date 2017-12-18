// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using SharpGen.Logging;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;

namespace SharpGen.Generator
{
    /// <summary>
    /// Transform a C++ method/function to a C# method.
    /// </summary>
    public class MethodTransform : TransformBase<CsMethod, CppMethod>, ITransform<CsFunction, CppFunction>
    {
        private static ExpressionSyntax GetCastedReturn(ExpressionSyntax invocation, CsMarshalBase returnType)
        {
            if (returnType.PublicType.Type != null && returnType.PublicType.Type == typeof(bool))
                return BinaryExpression(SyntaxKind.NotEqualsExpression,
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)),
                    invocation);
            if (returnType.PublicType is CsInterface)
                return ObjectCreationExpression(ParseTypeName(returnType.PublicType.QualifiedName),
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                CastExpression(QualifiedName(IdentifierName("System"), IdentifierName("IntPtr")), invocation)))),
                    InitializerExpression(SyntaxKind.ObjectInitializerExpression));
            if (returnType.PublicType.Type == typeof(string))
            {
                var marshalMethodName = "PtrToString" + (returnType.IsWideChar ? "Uni" : "Ansi");
                return InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        ParseTypeName("System.Runtime.InteropServices.Marshal"), IdentifierName(marshalMethodName)),
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    invocation
                                    ))));
            }
            return invocation;
        }


        private ExpressionSyntax GenerateInvocation(CsMethod method)
        {
            var arguments = new List<ExpressionSyntax>();

            if (method.IsReturnStructLarge)
            {
                arguments.Add(CastExpression(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                                        PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                            IdentifierName("__result__"))));
            }

            arguments.AddRange(method.Parameters.Select(param => ParseExpression(param.GetCallName(Manager.GlobalNamespace.GetTypeName("PointerSize")))));

            if (!(method is CsFunction))
            {
                arguments.Add(
                    ElementAccessExpression(
                        ParenthesizedExpression(
                            PrefixUnaryExpression(SyntaxKind.PointerIndirectionExpression,
                                CastExpression(PointerType(PointerType(PointerType(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword)))))),
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        ThisExpression(),
                                        IdentifierName("nativePointer"))))),
                        BracketedArgumentList(
                            SingletonSeparatedList(
                                Argument(method.CustomVtbl ?
                                (ExpressionSyntax)MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    ThisExpression(),
                                    IdentifierName($"{method.Name}__vtbl_index"))
                                : LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(method.Offset))
                                )
                            ))));
            }

            return GetCastedReturn(
                InvocationExpression(
                    IdentifierName(method is CsFunction ?
                        method.CppElementName + "_"
                    : method.Assembly.QualifiedName + ".LocalInterop." + method.Interop.Name),
                    ArgumentList(SeparatedList(arguments.Select(arg => Argument(arg))))),
                method.ReturnType
            );
        }

        public SyntaxNode GenerateCode(CsFunction csElement)
        {
            var interopFunction = csElement.Interop;

            var declaration = MethodDeclaration(ParseTypeName(interopFunction.ReturnType.TypeName), $"{interopFunction.Name}_")
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PrivateKeyword),
                        Token(SyntaxKind.UnsafeKeyword),
                        Token(SyntaxKind.StaticKeyword),
                        Token(SyntaxKind.ExternKeyword)))
                .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(
                    Attribute(
                            QualifiedName(
                                QualifiedName(
                                    QualifiedName(
                                        IdentifierName("System"),
                                        IdentifierName("Runtime")),
                                    IdentifierName("InteropServices")),
                                IdentifierName("DllImportAttribute")))
                        .WithArgumentList(
                            AttributeArgumentList(
                                SeparatedList(
                                    new []
                                    {
                                        AttributeArgument(
                                            IdentifierName(csElement.DllName)),
                                        AttributeArgument(
                                            LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                Literal(csElement.CppElementName)))
                                        .WithNameEquals(
                                            NameEquals(
                                                IdentifierName("EntryPoint"))),
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
                                                IdentifierName(csElement.CallingConvention)))
                                        .WithNameEquals(
                                            NameEquals(
                                                IdentifierName("CallingConvention")))
                                    })))))));

            return GenerateCode((CsMethod)csElement);
        }

        public override MemberDeclarationSyntax GenerateCode(CsMethod csElement)
        {
            // TODO: Output custom vtbl variable
            var vtblVariable = FieldDeclaration(
                VariableDeclaration(PredefinedType(Token(SyntaxKind.IntKeyword)),
                    SingletonSeparatedList(
                        VariableDeclarator($"{csElement.Name}__vtbl_index")
                            .WithInitializer(EqualsValueClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(csElement.Offset)))))))
                .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)));

            // Documentation
            var documentationTrivia = GenerateDocumentationTrivia(csElement);

            // method signature (commented if hidden)
            var methodDeclaration = MethodDeclaration(ParseTypeName(csElement.PublicReturnTypeQualifiedName), csElement.Name)
                .WithModifiers(TokenList(ParseTokens(csElement.VisibilityName)).Add(Token(SyntaxKind.UnsafeKeyword)))
                .WithParameterList(
                    ParameterList(
                        SeparatedList(
                            csElement.PublicParameters.Select(param =>
                                Parameter(Identifier(param.ParamName)) // TODO: Fix this to generate correct semantics
                                    .WithDefault(param.DefaultValue == null ? default
                                        :
                                        EqualsValueClause(ParseExpression(param.DefaultValue)))
                                )
                            )
                        )
                )
                .WithLeadingTrivia(Trivia(documentationTrivia));

            // If not hidden, generate body
            if (csElement.Hidden)
            {
                // return Comment(methodDeclaration.NormalizeWhitespace().ToFullString());
                return null;
            }

            var statements = new List<StatementSyntax>();

            // foreach parameter
            foreach (var parameter in csElement.Parameters)
            {
                statements.AddRange(GenerateParameterPrologue(parameter));
            }
            // predeclare return type
            if (csElement.HasReturnType)
            {
                statements.Add(LocalDeclarationStatement(
                    VariableDeclaration(
                        ParseTypeName(csElement.ReturnType.QualifiedName),
                        SingletonSeparatedList(
                            VariableDeclarator("__result__")))));
            }

            // handle fixed parameters
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
                } else if (param.IsFixed && param.IsValueType && !param.HasNativeValueType && !param.IsUsedAsReturnType)
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
            // Calli
            var invocation = GenerateInvocation(csElement);
            var callStmt = ExpressionStatement(csElement.HasReturnType && !csElement.IsReturnStructLarge ?
                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName("__result__"),
                    invocation)
                    : invocation);

            var fixedStatement = fixedStatements.FirstOrDefault()?.WithStatement(callStmt);
            foreach (var statement in fixedStatements.Skip(1))
            {
                fixedStatement = statement.WithStatement(fixedStatement);
            }

            statements.Add((StatementSyntax)fixedStatement ?? callStmt);

            foreach (var parameter in csElement.Parameters)
            {
                statements.AddRange(GenerateParameterEplilogue(parameter));
            }

            // Return
            if (csElement.HasPublicReturnType)
            {
                if ((csElement.ReturnType.PublicType.Name == Manager.GlobalNamespace.GetTypeName("Result")) && csElement.CheckReturnType)
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

            return methodDeclaration.WithBody(Block(statements));
        }

        private IEnumerable<StatementSyntax> GenerateParameterEplilogue(CsParameter param)
        {
            // Post-process output parameters
            if (param.IsOut)
            {
                if (param.HasNativeValueType)
                {
                    if (param.IsArray)
                    {
                        yield return LoopThroughArrayParameter(param.Name,
                            ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        ElementAccessExpression(
                                            IdentifierName(param.Name),
                                            BracketedArgumentList(
                                                SingletonSeparatedList(
                                                    Argument(IdentifierName("i"))))),
                                        IdentifierName("__MarshalFrom")),
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(
                                                ElementAccessExpression(
                                                    IdentifierName($"{param.TempName}_"),
                                                    BracketedArgumentList(
                                                        SingletonSeparatedList(
                                                            Argument(IdentifierName("i"))))))
                                                .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)))))),
                            "i");
                    }
                    else
                    {
                        yield return ExpressionStatement(
                            AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(param.Name),
                                ObjectCreationExpression(ParseTypeName(param.PublicType.QualifiedName))));
                        if (param.IsStaticMarshal)
                        {
                            yield return ExpressionStatement(
                                    InvocationExpression(
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            ParseTypeName(param.PublicType.QualifiedName),
                                            IdentifierName("__MarshalFrom")),
                                        ArgumentList(
                                            SeparatedList(
                                                new[]
                                                {
                                                    Argument(IdentifierName(param.Name))
                                                        .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                                                    Argument(IdentifierName(param.TempName))
                                                        .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword))
                                                }))));
                        }
                        else
                        {
                            yield return ExpressionStatement(
                                    InvocationExpression(
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(param.Name),
                                            IdentifierName("__MarshalFrom")),
                                        ArgumentList(
                                            SingletonSeparatedList(
                                                Argument(IdentifierName(param.TempName))
                                                    .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword))))));
                        }
                    }
                }
                else if (param.IsComObject)
                {
                    var paramInterface = param.PublicType as CsInterface;
                    if (param.IsArray)
                    {
                        yield return GenerateNullCheckIfNeeded(param, false,
                            LoopThroughArrayParameter(param.Name,
                                ExpressionStatement(
                                    AssignmentExpression(
                                        SyntaxKind.SimpleAssignmentExpression,
                                        ElementAccessExpression(
                                            IdentifierName(param.Name),
                                            BracketedArgumentList(
                                                SingletonSeparatedList(
                                                    Argument(
                                                        IdentifierName("i"))))),
                                        ConditionalExpression(
                                            ParenthesizedExpression(
                                                BinaryExpression(
                                                    SyntaxKind.EqualsExpression,
                                                    ElementAccessExpression(
                                                        IdentifierName(param.TempName),
                                                        BracketedArgumentList(
                                                            SingletonSeparatedList(
                                                                Argument(
                                                                    IdentifierName("i"))))),
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            IdentifierName("System"),
                                                            IdentifierName("IntPtr")),
                                                        IdentifierName("Zero")))),
                                            LiteralExpression(SyntaxKind.NullLiteralExpression),
                                            ObjectCreationExpression(
                                                IdentifierName(paramInterface.NativeImplem.QualifiedName))
                                                .WithArgumentList(
                                                    ArgumentList(
                                                        SingletonSeparatedList(
                                                            Argument(
                                                                ElementAccessExpression(
                                                                    IdentifierName(param.TempName),
                                                                    BracketedArgumentList(
                                                                        SingletonSeparatedList(
                                                                            Argument(
                                                                                IdentifierName("i")))))))))))),
                            "i"));
                    }
                    else
                    {
                        if (param.IsFastOut)
                        {
                            yield return ExpressionStatement(
                                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        ParenthesizedExpression(
                                            CastExpression(ParseTypeName(paramInterface.NativeImplem.QualifiedName),
                                                IdentifierName(param.Name))),
                                        IdentifierName("NativePointer")),
                                    IdentifierName(param.TempName)));
                        }
                        else
                        {
                            yield return ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    IdentifierName(param.Name),
                                    ConditionalExpression(
                                        ParenthesizedExpression(
                                            BinaryExpression(
                                                SyntaxKind.EqualsExpression,
                                                IdentifierName(param.TempName),
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("System"),
                                                        IdentifierName("IntPtr")),
                                                    IdentifierName("Zero")))),
                                        LiteralExpression(
                                            SyntaxKind.NullLiteralExpression),
                                        ObjectCreationExpression(
                                            IdentifierName(paramInterface.NativeImplem.QualifiedName))
                                        .WithArgumentList(
                                            ArgumentList(
                                                SingletonSeparatedList(
                                                    Argument(
                                                        IdentifierName(param.TempName))))))));
                        }
                    }
                }
                else if (param.IsBoolToInt && !param.IsArray)
                {
                    yield return ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(param.Name),
                            BinaryExpression(SyntaxKind.NotEqualsExpression,
                                IdentifierName(param.TempName),
                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))));
                }
            }
            else if (param.IsString && !param.IsWideChar)
            {
                yield return ExpressionStatement(
                    InvocationExpression(
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
                                IdentifierName("Marshal")),
                            IdentifierName("FreeHGlobal")),
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    IdentifierName(param.TempName))))));
            }
            // Free natively marshalled structs
            else if (param.HasNativeValueType)
            {
                if (param.IsArray)
                {
                    yield return GenerateNullCheckIfNeeded(param, false,
                        LoopThroughArrayParameter(param.Name,
                            ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        ElementAccessExpression(
                                            IdentifierName(param.Name),
                                            BracketedArgumentList(
                                                SingletonSeparatedList(
                                                    Argument(IdentifierName("i"))))),
                                        IdentifierName("__MarshalFree")),
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(
                                                ElementAccessExpression(
                                                    IdentifierName($"{param.TempName}_"),
                                                    BracketedArgumentList(
                                                        SingletonSeparatedList(
                                                            Argument(IdentifierName("i"))))))
                                                .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)))))),
                            "i"));

                }
                else
                {
                    if (param.IsStaticMarshal)
                    {
                        if (param.IsRef)
                        {
                            yield return ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        ParseTypeName(param.PublicType.QualifiedName),
                                        IdentifierName("__MarshalFrom")),
                                    ArgumentList(
                                        SeparatedList(
                                            new[]
                                            {
                                                Argument(IdentifierName(param.Name))
                                                    .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                                                Argument(IdentifierName(param.TempName))
                                                    .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword))
                                            }))));
                        }
                        yield return ExpressionStatement(
                            InvocationExpression(
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    ParseTypeName(param.PublicType.QualifiedName),
                                    IdentifierName("__MarshalFree")),
                                ArgumentList(
                                    SeparatedList(
                                        new[]
                                        {
                                            Argument(IdentifierName(param.Name))
                                                .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                                            Argument(IdentifierName(param.TempName))
                                                .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword))
                                        }))));
                    }
                    else
                    {
                        if (param.IsRef)
                        {
                            yield return GenerateNullCheckIfNeeded(param, true,
                                ExpressionStatement(
                                    InvocationExpression(
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(param.Name),
                                            IdentifierName("__MarshalFrom")),
                                        ArgumentList(
                                            SingletonSeparatedList(
                                                Argument(IdentifierName(param.TempName))
                                                    .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)))))));
                        }
                        yield return GenerateNullCheckIfNeeded(param, true,
                            ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(param.Name),
                                        IdentifierName("__MarshalFrom")),
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(IdentifierName(param.TempName))
                                                .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)))))));
                    }
                }
            }
        }

        private StatementSyntax GenerateNullCheckIfNeeded(CsParameter param, bool checkStructClass, StatementSyntax statement)
        {
            if (param.IsOptional && (!checkStructClass || param.IsStructClass))
            {
                return IfStatement(
                                BinaryExpression(SyntaxKind.NotEqualsExpression,
                                    IdentifierName(param.Name),
                                    LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                statement);
            }
            return statement;
        }

        private ExpressionSyntax GenerateNullCheckIfNeeded(CsParameter param, bool checkStructClass, ExpressionSyntax expression, ExpressionSyntax nullAlternative)
        {
            if (param.IsOptional && (!checkStructClass || param.IsStructClass))
            {
                return ConditionalExpression(
                    BinaryExpression(SyntaxKind.EqualsExpression,
                        IdentifierName(param.Name),
                        LiteralExpression(SyntaxKind.NullLiteralExpression)),
                        nullAlternative,
                        expression);
            }
            return expression;
        }

        private StatementSyntax LoopThroughArrayParameter(string parameterName, StatementSyntax loopBody, string variableName)
        {
            return ForStatement(loopBody)
                .WithDeclaration(
                    VariableDeclaration(
                        PredefinedType(
                            Token(SyntaxKind.IntKeyword)),
                        SingletonSeparatedList(
                            VariableDeclarator(
                                Identifier(variableName))
                            .WithInitializer(
                                EqualsValueClause(
                                    LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        Literal(0)))))))
                .WithCondition(
                    BinaryExpression(
                        SyntaxKind.LessThanExpression,
                        IdentifierName(variableName),
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(parameterName),
                            IdentifierName("Length"))))
                .WithIncrementors(
                    SingletonSeparatedList<ExpressionSyntax>(
                        PostfixUnaryExpression(
                            SyntaxKind.PostIncrementExpression,
                            IdentifierName(variableName))));
        }

        private IEnumerable<StatementSyntax> GenerateParameterPrologue(CsParameter param)
        {
            // predeclare return type parameter
            if (param.IsUsedAsReturnType)
            {
                yield return LocalDeclarationStatement(
                    VariableDeclaration(
                        param.IsArray ? ArrayType(ParseTypeName(param.PublicType.QualifiedName)) : ParseTypeName(param.PublicType.QualifiedName),
                        SingletonSeparatedList(
                            VariableDeclarator(param.Name))));
            }
            // In-Optional parameters
            if (param.IsArray && param.IsValueType && !param.HasNativeValueType)
            {
                if (param.IsOptional)
                {
                    yield return LocalDeclarationStatement(
                        VariableDeclaration(
                            ArrayType(ParseTypeName(param.PublicType.QualifiedName)),
                            SingletonSeparatedList(
                                VariableDeclarator($"{param.TempName}_")
                                    .WithInitializer(EqualsValueClause(IdentifierName(param.Name))))));
                }
                yield break;
            }
            // handle native marshalling if needed
            if (param.HasNativeValueType)
            {
                if (param.IsArray)
                {
                    yield return LocalDeclarationStatement(
                        VariableDeclaration(
                            ArrayType(ParseTypeName($"{param.PublicType.QualifiedName}.__Native")),
                            SingletonSeparatedList(
                                VariableDeclarator($"{param.TempName}_")
                                    .WithInitializer(EqualsValueClause(
                                        GenerateNullCheckIfNeeded(param, false,
                                            ObjectCreationExpression(
                                                ArrayType(ParseTypeName($"{param.PublicType.QualifiedName}.__Native"),
                                                SingletonList(ArrayRankSpecifier(
                                                    SingletonSeparatedList<ExpressionSyntax>(
                                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                            IdentifierName(param.Name),
                                                            IdentifierName("Length"))))))),
                                            LiteralExpression(SyntaxKind.NullLiteralExpression)))))));
                    if (param.IsRefIn)
                    {
                        yield return GenerateNullCheckIfNeeded(param, false,
                                    LoopThroughArrayParameter(param.Name, ExpressionStatement(
                                            InvocationExpression(
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    ElementAccessExpression(
                                                        IdentifierName(param.Name),
                                                        BracketedArgumentList(
                                                            SingletonSeparatedList(
                                                                Argument(IdentifierName("i"))))),
                                                    IdentifierName("__MarshalTo")),
                                                    ArgumentList(
                                                                SingletonSeparatedList(
                                                                    Argument(
                                                                        ElementAccessExpression(
                                                                            IdentifierName($"{param.TempName}_"))
                                                                        .WithArgumentList(
                                                                            BracketedArgumentList(
                                                                                SingletonSeparatedList(
                                                                                    Argument(
                                                                                        IdentifierName("i"))))))
                                                                    .WithRefOrOutKeyword(
                                                                        Token(SyntaxKind.RefKeyword)))))), "i"));

                    }
                }
                else
                {
                    yield return LocalDeclarationStatement(
                        VariableDeclaration(IdentifierName("var"),
                            SingletonSeparatedList(
                                VariableDeclarator(param.TempName)
                                    .WithInitializer(
                                        EqualsValueClause(
                                            ParseExpression(((CsStruct)param.PublicType).GetConstructor()))))));
                    if (param.IsRefIn || param.IsRef || param.IsIn)
                    {
                        if (param.IsStaticMarshal)
                        {
                            yield return GenerateNullCheckIfNeeded(param, false,
                                ExpressionStatement(
                                    InvocationExpression(
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            ParseTypeName(param.PublicType.QualifiedName),
                                            IdentifierName("__MarshalTo")),
                                        ArgumentList(
                                            SeparatedList(
                                                new[]
                                                {
                                                    Argument(IdentifierName(param.Name))
                                                        .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                                                    Argument(IdentifierName(param.TempName))
                                                        .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword))
                                                })))));
                        }
                        else
                        {
                            yield return GenerateNullCheckIfNeeded(param, true,
                                ExpressionStatement(
                                    InvocationExpression(
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(param.Name),
                                            IdentifierName("__MarshalTo")),
                                        ArgumentList(
                                            SingletonSeparatedList(
                                                Argument(IdentifierName(param.TempName))
                                                    .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)))))));
                        }
                    }
                }
            }
            // handle out parameters
            else if (param.IsOut)
            {
                if (param.IsValueType && !param.IsPrimitive)
                {
                    yield return ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(param.Name),
                            ObjectCreationExpression(ParseTypeName(param.PublicType.QualifiedName))
                    ));
                }
                else if (param.IsBoolToInt && !param.IsArray)
                {
                    yield return LocalDeclarationStatement(
                        VariableDeclaration(PredefinedType(Token(SyntaxKind.IntKeyword)),
                            SingletonSeparatedList(VariableDeclarator(param.TempName))));
                }
                else if (param.IsComObject)
                {
                    if (param.IsArray)
                    {
                        yield return LocalDeclarationStatement(
                            VariableDeclaration(
                                PointerType(
                                    QualifiedName(
                                        IdentifierName("System"),
                                        IdentifierName("IntPtr"))),
                                SingletonSeparatedList(
                                    VariableDeclarator(param.TempName)
                                        .WithInitializer(
                                            EqualsValueClause(
                                                StackAllocArrayCreationExpression(
                                                    ArrayType(
                                                        QualifiedName(
                                                            IdentifierName("System"),
                                                            IdentifierName("IntPtr")),
                                                        SingletonList(
                                                            ArrayRankSpecifier(
                                                                SingletonSeparatedList(
                                                                    GenerateNullCheckIfNeeded(param, true,
                                                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                            IdentifierName(param.Name),
                                                                            IdentifierName("Length")),
                                                                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))
                                )))))))))));
                    }
                    else
                    {
                        yield return LocalDeclarationStatement(
                            VariableDeclaration(
                                QualifiedName(
                                    IdentifierName("System"),
                                    IdentifierName("IntPtr")),
                                SingletonSeparatedList(
                                    VariableDeclarator(param.TempName)
                                        .WithInitializer(
                                            EqualsValueClause(
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("System"),
                                                        IdentifierName("IntPtr")),
                                                    IdentifierName("Zero")))))));
                    }
                }
            }
            // handle array [In] parameters
            else if (param.IsArray)
            {
                if (param.IsComObject)
                {
                    yield return LocalDeclarationStatement(
                        VariableDeclaration(
                            PointerType(
                                QualifiedName(
                                    IdentifierName("System"),
                                    IdentifierName("IntPtr"))),
                            SingletonSeparatedList(
                                VariableDeclarator(param.TempName))));
                    if (param.IsIn)
                    {
                        yield return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(param.TempName),
                                CastExpression(
                                    PointerType(
                                        IdentifierName("IntPtr")),
                                    LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        Literal(0)))));
                        yield return GenerateNullCheckIfNeeded(param, false,
                            Block(
                            new StatementSyntax[] {
                                LocalDeclarationStatement(
                                    VariableDeclaration(
                                        PointerType(
                                            QualifiedName(
                                                IdentifierName("System"),
                                                IdentifierName("IntPtr"))),
                                        SingletonSeparatedList(
                                            VariableDeclarator(
                                                Identifier($"{param.TempName}_"))
                                            .WithInitializer(
                                                EqualsValueClause(
                                                    StackAllocArrayCreationExpression(
                                                        ArrayType(
                                                            QualifiedName(
                                                                IdentifierName("System"),
                                                                IdentifierName("IntPtr")),
                                                            SingletonList(
                                                                    ArrayRankSpecifier(
                                                                        SingletonSeparatedList<ExpressionSyntax>(
                                                                            MemberAccessExpression(
                                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                                IdentifierName(param.Name),
                                                                                IdentifierName("Length")))))))))))),
                                ExpressionStatement(
                                    AssignmentExpression(
                                        SyntaxKind.SimpleAssignmentExpression,
                                        IdentifierName(param.TempName),
                                        IdentifierName($"{param.TempName}_"))),
                                LoopThroughArrayParameter(param.Name,
                                    ExpressionStatement(
                                        AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            ElementAccessExpression(
                                                IdentifierName(param.TempName),
                                                BracketedArgumentList(
                                                    SingletonSeparatedList(
                                                        Argument(
                                                            IdentifierName("i"))))),
                                            BinaryExpression(
                                                SyntaxKind.CoalesceExpression,
                                                ConditionalAccessExpression(
                                                    ElementAccessExpression(
                                                        IdentifierName(param.Name),
                                                        BracketedArgumentList(
                                                            SingletonSeparatedList(
                                                                Argument(
                                                                    IdentifierName("i"))))),
                                                    MemberBindingExpression(
                                                        IdentifierName("NativePointer"))),
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("System"),
                                                        IdentifierName("IntPtr")),
                                                    IdentifierName("Zero"))))),
                                    "i")
                            }));
                    }
                    else
                    {
                        yield return LocalDeclarationStatement(
                            VariableDeclaration(
                                PointerType(
                                    QualifiedName(
                                        IdentifierName("System"),
                                        IdentifierName("IntPtr"))),
                                SingletonSeparatedList(
                                    VariableDeclarator(
                                        Identifier($"{param.TempName}_"))
                                    .WithInitializer(
                                        EqualsValueClause(
                                            StackAllocArrayCreationExpression(
                                                ArrayType(
                                                    QualifiedName(
                                                        IdentifierName("System"),
                                                        IdentifierName("IntPtr")),
                                                    SingletonList(
                                                            ArrayRankSpecifier(
                                                                SingletonSeparatedList<ExpressionSyntax>(
                                                                    MemberAccessExpression(
                                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                                        IdentifierName(param.Name),
                                                                        IdentifierName("Length"))))))))))));
                            yield return ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    IdentifierName(param.TempName),
                                    IdentifierName($"{param.TempName}_")));

                    }
                }
            }
            // handle string parameters
            else if (param.IsString && !param.IsWideChar)
            {
                yield return LocalDeclarationStatement(
                    VariableDeclaration(
                        QualifiedName(
                            IdentifierName("System"),
                            IdentifierName("IntPtr")),
                        SingletonSeparatedList(
                            VariableDeclarator(param.TempName)
                                .WithInitializer(
                                    EqualsValueClause(
                                        InvocationExpression(
                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                ParseTypeName(Manager.GlobalNamespace.GetTypeName("Utilities")),
                                                IdentifierName("StringToHGlobalAnsi")),
                                            ArgumentList(
                                                SingletonSeparatedList(
                                                    Argument(IdentifierName(param.Name))))))))));
            }
            else if (param.IsRefIn && param.IsValueType && param.IsOptional)
            {
                yield return LocalDeclarationStatement(
                    VariableDeclaration(ParseTypeName(param.PublicType.QualifiedName),
                        SingletonSeparatedList(
                            VariableDeclarator(param.TempName))));
                yield return GenerateNullCheckIfNeeded(param, false,
                    ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(param.TempName),
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(param.Name),
                                IdentifierName("Value")))));
            }
        }

        /// <summary>
        /// Prepares the specified C++ element to a C# element.
        /// </summary>
        /// <param name="cppMethod">The C++ element.</param>
        /// <returns>The C# element created and registered to the <see cref="TransformManager"/></returns>
        public override CsMethod Prepare(CppMethod cppMethod) => new CsMethod(cppMethod);

        public CsFunction Prepare(CppFunction cppFunction)
        {
            var cSharpFunction = new CsFunction(cppFunction);
            // All functions must have a tag
            var tag = cppFunction.GetTagOrDefault<MappingRule>();

            if (tag == null || tag.CsClass == null)
            {
                Logger.Error("CppFunction [{0}] is not tagged and attached to any Class/FunctionGroup", cppFunction);
                return null;
            }

            var csClass = Manager.FindCsClassContainer(tag.CsClass);

            if (csClass == null)
            {
                Logger.Error("CppFunction [{0}] is not attached to a Class/FunctionGroup", cppFunction);
                return null;
            }

            // Set the DllName for this function
            cSharpFunction.DllName = tag.FunctionDllName;

            // Add the function to the ClassType
            csClass.Add(cSharpFunction);

            // Map the C++ name to the CSharpType
            Manager.BindType(cppFunction.Name, cSharpFunction);

            return cSharpFunction;
        }

        /// <summary>
        /// Processes the specified C# element to complete the mapping process between the C++ and C# element.
        /// </summary>
        /// <param name="csElement">The C# element.</param>
        public override void Process(CsMethod csElement)
        {
            try
            {
                var csMethod = csElement;
                Logger.PushContext("Method {0}", csMethod.CppElement);

                ProcessMethod(csMethod);

                RegisterNativeInteropSignature(csMethod);
            }
            finally
            {
                Logger.PopContext();
            }
        }

        public void Process(CsFunction csFunction)
        {
            csFunction.Visibility = csFunction.Visibility | Visibility.Static;
            Process((CsMethod)csFunction);
        }

        /// <summary>
        /// Processes the specified method.
        /// </summary>
        /// <param name="method">The method.</param>
        private void ProcessMethod(CsMethod method)
        {
            var cppMethod = (CppMethod)method.CppElement;

            method.Name = NamingRules.Rename(cppMethod);
            method.Offset = cppMethod.Offset;

            // For methods, the tag "type" is only used for return type
            // So we are overriding the return type here
            var tag = cppMethod.GetTagOrDefault<MappingRule>();
            if (tag.MappingType != null)
                cppMethod.ReturnType.Tag = new MappingRule { MappingType = tag.MappingType };

            // Apply any offset to the method's vtable
            method.Offset += tag.LayoutOffsetTranslate;

            // Get the inferred return type
            method.ReturnType = Manager.GetCsType<CsMarshalBase>(cppMethod.ReturnType);

            // Hide return type only if it is a HRESULT and AlwaysReturnHResult is false
            if (method.CheckReturnType && method.ReturnType.PublicType != null &&
                method.ReturnType.PublicType.QualifiedName == Manager.GlobalNamespace.GetTypeName("Result"))
            {
                method.HideReturnType = !method.AlwaysReturnHResult;
            }

            // Iterates on parameters to convert them to C# parameters
            foreach (var cppParameter in cppMethod.Parameters)
            {
                var cppAttribute = cppParameter.Attribute;
                var paramTag = cppParameter.GetTagOrDefault<MappingRule>();

                bool hasArray = cppParameter.IsArray || ((cppAttribute & ParamAttribute.Buffer) != 0);
                bool hasParams = (cppAttribute & ParamAttribute.Params) == ParamAttribute.Params;
                bool isOptional = (cppAttribute & ParamAttribute.Optional) != 0;

                var paramMethod = Manager.GetCsType<CsParameter>(cppParameter);

                paramMethod.Name = NamingRules.Rename(cppParameter);

                bool hasPointer = paramMethod.HasPointer;

                var publicType = paramMethod.PublicType;
                var marshalType = paramMethod.MarshalType;

                CsParameterAttribute parameterAttribute = CsParameterAttribute.In;

                if (hasArray)
                    hasPointer = true;

                // --------------------------------------------------------------------------------
                // Pointer - Handle special cases
                // --------------------------------------------------------------------------------
                if (hasPointer)
                {
                    marshalType = Manager.ImportType(typeof(IntPtr));

                    // --------------------------------------------------------------------------------
                    // Handling Parameter Interface
                    // --------------------------------------------------------------------------------
                    if (publicType is CsInterface)
                    {
                        // Force Interface** to be ParamAttribute.Out when None
                        if (cppAttribute == ParamAttribute.In)
                        {
                            if (cppParameter.Pointer == "**")
                                cppAttribute = ParamAttribute.Out;
                        }

                        if ((cppAttribute & ParamAttribute.In) != 0 || (cppAttribute & ParamAttribute.InOut) != 0)
                        {
                            parameterAttribute = CsParameterAttribute.In;

                            // Force all array of interface to support null
                            if (hasArray)
                            {
                                isOptional = true;
                            }

                            // If Interface is a callback, use IntPtr as a public marshalling type
                            CsInterface publicCsInterface = (CsInterface)publicType;
                            if (publicCsInterface.IsCallback)
                            {
                                publicType = Manager.ImportType(typeof(IntPtr));
                                // By default, set the Visibility to internal for methods using callbacks
                                // as we need to provide user method. Don't do this on functions as they
                                // are already hidden by the container
                                if (!(method is CsFunction))
                                {
                                    method.Visibility = Visibility.Internal;
                                    method.Name = method.Name + "_";
                                }
                            }
                        }
                        //else if ((cppParameter.Attribute & ParamAttribute.InOut) != 0)
                        //    parameterAttribute = method.ParameterAttribute.Ref;
                        else if ((cppAttribute & ParamAttribute.Out) != 0)
                            parameterAttribute = CsParameterAttribute.Out;
                    }
                    else
                    {
                        // If a pointer to array of bool are handle as array of int
                        if (paramMethod.IsBoolToInt && (cppAttribute & ParamAttribute.Buffer) != 0)
                            publicType = Manager.ImportType(typeof(int));

                        // --------------------------------------------------------------------------------
                        // Handling Parameter Interface
                        // --------------------------------------------------------------------------------


                        if ((cppAttribute & ParamAttribute.In) != 0)
                        {
                            parameterAttribute = publicType.Type == typeof(IntPtr) || publicType.Name == Manager.GlobalNamespace.GetTypeName("FunctionCallback") ||
                                                 publicType.Type == typeof(string)
                                                     ? CsParameterAttribute.In
                                                     : CsParameterAttribute.RefIn;
                        }
                        else if ((cppAttribute & ParamAttribute.InOut) != 0)
                        {
                            if ((cppAttribute & ParamAttribute.Optional) != 0)
                            {
                                publicType = Manager.ImportType(typeof(IntPtr));
                                parameterAttribute = CsParameterAttribute.In;
                            }
                            else
                            {
                                parameterAttribute = CsParameterAttribute.Ref;
                            }

                        }
                        else if ((cppAttribute & ParamAttribute.Out) != 0)
                            parameterAttribute = CsParameterAttribute.Out;

                        // Handle void* with Buffer attribute
                        if (cppParameter.TypeName == "void" && (cppAttribute & ParamAttribute.Buffer) != 0)
                        {
                            hasArray = false;
                            parameterAttribute = CsParameterAttribute.In;
                        }
                        else if (publicType.Type == typeof(string) && (cppAttribute & ParamAttribute.Out) != 0)
                        {
                            publicType = Manager.ImportType(typeof(IntPtr));
                            parameterAttribute = CsParameterAttribute.In;
                            hasArray = false;
                        }
                        else if (publicType is CsStruct structType &&
                                 (parameterAttribute == CsParameterAttribute.Out || hasArray || parameterAttribute == CsParameterAttribute.RefIn || parameterAttribute == CsParameterAttribute.Ref))
                        {
                            // Set IsOut on structure to generate proper marshalling
                            structType.IsOut = true;
                        }
                    }
                }
                else if (publicType is CsStruct structType && parameterAttribute != CsParameterAttribute.Out)
                {
                    structType.IsOut = true;
                }

                paramMethod.HasPointer = hasPointer;
                paramMethod.Attribute = parameterAttribute;
                paramMethod.IsArray = hasArray;
                paramMethod.HasParams = hasParams;
                paramMethod.HasPointer = hasPointer;
                paramMethod.PublicType = publicType ?? throw new ArgumentException("Public type cannot be null");
                paramMethod.MarshalType = marshalType;
                paramMethod.IsOptional = isOptional;

                // Force IsString to be only string (due to Buffer attribute)
                if (paramMethod.IsString)
                    paramMethod.IsArray = false;

                method.Add(paramMethod);
            }
        }

        /// <summary>
        /// Registers the native interop signature.
        /// </summary>
        /// <param name="csMethod">The cs method.</param>
        private void RegisterNativeInteropSignature(CsMethod csMethod)
        {
            // Tag if the method is a function
            var cSharpInteropCalliSignature = new InteropMethodSignature { IsFunction = (csMethod is CsFunction) };

            // Handle Return Type parameter
            // MarshalType.Type == null, then check that it is a structure
            if (csMethod.ReturnType.PublicType is CsStruct || csMethod.ReturnType.PublicType is CsEnum)
            {
                // Return type and 1st parameter are implicitly a pointer to the structure to fill 
                if (csMethod.IsReturnStructLarge)
                {
                    cSharpInteropCalliSignature.ReturnType = typeof(void*);
                    cSharpInteropCalliSignature.ParameterTypes.Add(typeof(void*));
                }
                else
                {
                    // Patch for Mono bug with structs marshalling and calli.
                    var returnQualifiedName = csMethod.ReturnType.PublicType.QualifiedName;
                    if (returnQualifiedName == Manager.GlobalNamespace.GetTypeName("Result"))
                        cSharpInteropCalliSignature.ReturnType = typeof(int);
                    else if (returnQualifiedName == Manager.GlobalNamespace.GetTypeName("PointerSize"))
                        cSharpInteropCalliSignature.ReturnType = typeof(void*);
                    else
                        cSharpInteropCalliSignature.ReturnType = csMethod.ReturnType.PublicType.QualifiedName;
                }
            }
            else if (csMethod.ReturnType.MarshalType.Type != null)
            {
                Type type = csMethod.ReturnType.MarshalType.Type;
                cSharpInteropCalliSignature.ReturnType = type;
            }
            else
            {
                throw new ArgumentException(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Invalid return type {0} for method {1}", csMethod.ReturnType.PublicType.QualifiedName, csMethod.CppElement));
            }

            // Handle Parameters
            foreach (var param in csMethod.Parameters)
            {
                InteropType interopType;
                string publicName = param.PublicType.QualifiedName;
                // Patch for Mono bug with structs marshalling and calli.
                if (publicName == Manager.GlobalNamespace.GetTypeName("PointerSize"))
                {
                    interopType = typeof(void*);
                }
                else if (param.MarshalType.Type == null)
                {
                    if (param.PublicType is CsStruct)
                    {
                        // If parameter is a struct, then a LocalInterop is needed
                        interopType = param.PublicType.QualifiedName;
                        cSharpInteropCalliSignature.IsLocal = true;
                    }
                    else
                    {
                        throw new ArgumentException(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Invalid parameter {0} for method {1}", param.PublicType.QualifiedName, csMethod.CppElement));
                    }
                }
                else
                {
                    Type type = param.MarshalType.Type;
                    // Patch for Mono bug with structs marshalling and calli.
                    if (type == typeof(IntPtr))
                        type = typeof(void*);
                    interopType = type;
                }

                cSharpInteropCalliSignature.ParameterTypes.Add(interopType);
            }

            var assembly = csMethod.GetParent<CsAssembly>();
            cSharpInteropCalliSignature = assembly.Interop.Add(cSharpInteropCalliSignature);

            csMethod.Interop = cSharpInteropCalliSignature;
        }
    }
}