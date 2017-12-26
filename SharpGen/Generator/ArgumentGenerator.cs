﻿using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace SharpGen.Generator
{
    class ArgumentGenerator : ICodeGenerator<CsParameter, ArgumentSyntax>
    {
        GlobalNamespaceProvider GlobalNamespace;

        public ArgumentSyntax GenerateCode(CsParameter csElement)
        {
            return Argument(GenerateExpression(csElement));
        }

        private ExpressionSyntax GenerateExpression(CsParameter param)
        {
            if (param.IsComArray)
            {
                return CastExpression(
                    PointerType(
                        PredefinedType(
                            Token(SyntaxKind.VoidKeyword))),
                    ParenthesizedExpression(
                        BinaryExpression(
                            SyntaxKind.CoalesceExpression,
                            ConditionalAccessExpression(
                                IdentifierName(param.Name),
                                MemberBindingExpression(
                                    IdentifierName("NativePointer"))),
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("IntPtr"),
                                IdentifierName("Zero")))));
            }
            else if (param.IsOut)
            {
                if (param.PublicType is CsInterface)
                {
                    if (param.IsArray)
                    {
                        if (param.IsOptional)
                        {
                            return ConditionalExpression(
                                BinaryExpression(
                                    SyntaxKind.EqualsExpression,
                                    IdentifierName(param.Name),
                                    LiteralExpression(
                                        SyntaxKind.NullLiteralExpression)),
                                CastExpression(
                                    PointerType(
                                        PredefinedType(
                                            Token(SyntaxKind.VoidKeyword))),
                                    LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        Literal(0))),
                                IdentifierName(param.TempName));
                        }
                        else
                        {
                            return IdentifierName(param.TempName);
                        }
                    }
                    else
                    {
                        return PrefixUnaryExpression(SyntaxKind.AddressOfExpression, IdentifierName(param.TempName));
                    }
                }
                else if (param.IsArray)
                {
                    return param.IsComArray ? IdentifierName(param.Name) : IdentifierName(param.TempName);
                }
                else if (param.IsFixed && !param.HasNativeValueType)
                {
                    return param.IsUsedAsReturnType
                        ? PrefixUnaryExpression(SyntaxKind.AddressOfExpression, IdentifierName(param.Name))
                        : (ExpressionSyntax)IdentifierName(param.TempName);
                }
                else if (param.HasNativeValueType || param.IsBoolToInt)
                {
                    return PrefixUnaryExpression(SyntaxKind.AddressOfExpression, IdentifierName(param.TempName));
                }
                else
                {
                    return param.IsValueType
                        ? PrefixUnaryExpression(SyntaxKind.AddressOfExpression, IdentifierName(param.Name))
                        : (ExpressionSyntax)IdentifierName(param.TempName);
                }
            }
            else if (param.IsRefInValueTypeOptional)
            {
                return ConditionalExpression(
                    BinaryExpression(
                        SyntaxKind.EqualsExpression,
                        IdentifierName(param.Name),
                        LiteralExpression(
                            SyntaxKind.NullLiteralExpression)),
                    CastExpression(
                        PointerType(
                            PredefinedType(
                                Token(SyntaxKind.VoidKeyword))),
                        LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            Literal(0))),
                    IdentifierName(param.TempName));
            }
            else if (param.IsRefInValueTypeByValue)
            {
                return PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                    IdentifierName(param.HasNativeValueType ? param.TempName : param.Name));
            }
            else if (!param.IsFixed && param.PublicType is CsEnum && !param.IsArray)
            {
                return CheckedExpression(
                    SyntaxKind.UncheckedExpression,
                    CastExpression(
                        PredefinedType(
                            Token(SyntaxKind.IntKeyword)),
                        IdentifierName(param.TempName)));
            }
            else if (param.PublicType.Type == typeof(string))
            {
                return CastExpression(
                    PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                    IdentifierName(param.TempName));
            }
            else if (param.PublicType is CsInterface
                && param.Attribute == CsParameterAttribute.In
                && !param.IsArray)
            {
                return CastExpression(
                    PointerType(
                        PredefinedType(
                            Token(SyntaxKind.VoidKeyword))),
                    ParenthesizedExpression(
                        BinaryExpression(
                            SyntaxKind.CoalesceExpression,
                            ConditionalAccessExpression(
                                IdentifierName(param.Name),
                                MemberBindingExpression(
                                    IdentifierName("NativePointer"))),
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("IntPtr"),
                                IdentifierName("Zero")))));
            }
            else if (param.IsArray)
            {
                if (param.HasNativeValueType
                    || param.IsBoolToInt
                    || (param.IsValueType && param.IsOptional))
                {
                    return IdentifierName(param.TempName);
                }
            }
            else if (param.IsBoolToInt)
            {
                return ConditionalExpression(IdentifierName(param.Name),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)));
            }
            else if (param.IsFixed && !param.HasNativeValueType)
            {
                return IdentifierName(param.TempName);
            }
            else if (param.PublicType.Type == typeof(IntPtr) && !param.IsArray)
            {
                return CastExpression(
                    PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                    IdentifierName(param.Name));
            }
            else if (param.HasNativeValueType)
            {
                return param.IsIn ?
                    (ExpressionSyntax)IdentifierName(param.TempName)
                    : PrefixUnaryExpression(SyntaxKind.AddressOfExpression, IdentifierName(param.TempName));
            }
            else if (param.PublicType.QualifiedName == GlobalNamespace.GetTypeName("PointerSize"))
            {
                return CastExpression(
                    PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                    IdentifierName(param.Name));
            }
            return IdentifierName(param.Name);
        }
    }
}