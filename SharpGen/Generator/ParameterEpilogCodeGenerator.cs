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
                            CreateMarshalStructStatement(
                                param,
                                "__MarshalFrom",
                                ElementAccessExpression(
                                    IdentifierName(param.Name),
                                    BracketedArgumentList(
                                        SingletonSeparatedList(
                                            Argument(IdentifierName("i"))))),
                                ElementAccessExpression(
                                    IdentifierName($"{param.TempName}_"),
                                    BracketedArgumentList(
                                        SingletonSeparatedList(
                                            Argument(IdentifierName("i")))))),
                            "i");                
                    }
                    else
                    {
                        yield return ExpressionStatement(
                            AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(param.Name),
                                ObjectCreationExpression(ParseTypeName(param.PublicType.QualifiedName))
                                    .WithArgumentList(ArgumentList())));
                        
                        yield return CreateMarshalStructStatement(
                                param,
                                "__MarshalFrom",
                                IdentifierName(param.Name),
                                IdentifierName(param.TempName));
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
                        yield return LoopThroughArrayParameter(param.Name,
                            CreateMarshalStructStatement(
                                param,
                                "__MarshalFree",
                                ElementAccessExpression(
                                    IdentifierName(param.Name),
                                    BracketedArgumentList(
                                        SingletonSeparatedList(
                                            Argument(IdentifierName("i"))))),
                                ElementAccessExpression(
                                    IdentifierName($"{param.TempName}_"),
                                    BracketedArgumentList(
                                        SingletonSeparatedList(
                                            Argument(IdentifierName("i")))))),
                            "i");

                }
                else
                {
                    ExpressionSyntax publicElementExpression = IdentifierName(param.Name);

                    if (param.IsNullableStruct)
                    {
                        publicElementExpression = MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            publicElementExpression,
                            IdentifierName("Value"));
                    }

                    if (param.IsRef)
                    {
                        yield return CreateMarshalStructStatement(
                            param,
                            "__MarshalFrom",
                            publicElementExpression,
                            IdentifierName(param.TempName)
                        );
                    }

                    yield return CreateMarshalStructStatement(
                        param,
                        "__MarshalFree",
                        publicElementExpression,
                        IdentifierName(param.TempName)
                    );
                }
            }
        }

    }
}
