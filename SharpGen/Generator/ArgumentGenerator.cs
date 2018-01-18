using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        GlobalNamespaceProvider globalNamespace;

        public ArgumentGenerator(GlobalNamespaceProvider globalNamespace)
        {
            this.globalNamespace = globalNamespace;
        }

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
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("System"),
                                    IdentifierName("IntPtr")),
                                IdentifierName("Zero")))));
            }
            if (param.IsOut)
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
            if (param.IsRefInValueTypeOptional)
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
                    PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                        IdentifierName(param.TempName)));
            }
            if (param.IsRefInValueTypeByValue)
            {
                return PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                    IdentifierName(param.HasNativeValueType ? param.TempName : param.Name));
            }
            if (!param.IsFixed && param.PublicType is CsEnum && !param.IsArray)
            {
                return CheckedExpression(
                    SyntaxKind.UncheckedExpression,
                    CastExpression(
                        PredefinedType(
                            Token(SyntaxKind.IntKeyword)),
                        IdentifierName(param.Name)));
            }

            var fundamental = param.PublicType as CsFundamentalType;

            if (fundamental?.Type == typeof(string))
            {
                return CastExpression(
                    PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                    IdentifierName(param.TempName));
            }
            if (param.PublicType is CsInterface
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
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("System"),
                                    IdentifierName("IntPtr")),
                                IdentifierName("Zero")))));
            }
            if (param.IsArray)
            {
                if (param.HasNativeValueType
                    || param.IsBoolToInt
                    || (param.IsValueType && param.IsOptional))
                {
                    return IdentifierName(param.TempName);
                }
            }
            if (param.IsBoolToInt)
            {
                return ConditionalExpression(IdentifierName(param.Name),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)));
            }
            if (param.IsFixed && !param.HasNativeValueType)
            {
                return IdentifierName(param.TempName);
            }
            if (fundamental?.Type == typeof(IntPtr) && !param.IsArray)
            {
                return CastExpression(
                    PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                    IdentifierName(param.Name));
            }
            if (param.HasNativeValueType)
            {
                return param.IsIn ?
                    (ExpressionSyntax)IdentifierName(param.TempName)
                    : PrefixUnaryExpression(SyntaxKind.AddressOfExpression, IdentifierName(param.TempName));
            }
            if (param.PublicType.QualifiedName == globalNamespace.GetTypeName("PointerSize"))
            {
                return CastExpression(
                    PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                    IdentifierName(param.Name));
            }
            return IdentifierName(param.Name);
        }
    }
}
