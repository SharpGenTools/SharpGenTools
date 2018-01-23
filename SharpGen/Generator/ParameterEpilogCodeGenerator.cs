using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace SharpGen.Generator
{
    class ParameterEpilogCodeGenerator : ParameterPrologEpilogBase, IMultiCodeGenerator<CsParameter, StatementSyntax>
    {
        public IEnumerable<StatementSyntax> GenerateCode(CsParameter param)
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
                                ObjectCreationExpression(ParseTypeName(param.PublicType.QualifiedName))
                                    .WithArgumentList(ArgumentList())));
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
                else if (param.IsInterface)
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
                                                IdentifierName(paramInterface.GetNativeImplementationOrThis().QualifiedName))
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
                                            CastExpression(ParseTypeName(paramInterface.GetNativeImplementationOrThis().QualifiedName),
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
                                            IdentifierName(paramInterface.GetNativeImplementationOrThis().QualifiedName))
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

    }
}
