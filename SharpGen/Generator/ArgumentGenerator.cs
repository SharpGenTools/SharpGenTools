using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace SharpGen.Generator
{
    class ArgumentGenerator : MarshallingCodeGeneratorBase, ICodeGenerator<CsMarshalCallableBase, ArgumentSyntax>
    {
        GlobalNamespaceProvider globalNamespace;

        public ArgumentGenerator(GlobalNamespaceProvider globalNamespace)
            :base(globalNamespace)
        {
            this.globalNamespace = globalNamespace;
        }

        public ArgumentSyntax GenerateCode(CsMarshalCallableBase csElement)
        {
            if (csElement.MarshalType.QualifiedName == "System.IntPtr") // Marshal System.IntPtr as void* for arguments.
            {
                return Argument(
                    CastExpression(
                        PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                        ParenthesizedExpression(GenerateExpression(csElement))));
            }

            // Cast the argument to the native (marshal) type
            if (csElement.MarshalType != csElement.PublicType && !(csElement is CsReturnValue))
            {
                return Argument(CheckedExpression(
                            SyntaxKind.UncheckedExpression,
                            CastExpression(
                                ParseTypeName(csElement.MarshalType.QualifiedName),
                                GenerateExpression(csElement)))); 
            }
            else
            {
                return Argument(GenerateExpression(csElement));
            }
        }

        private ExpressionSyntax GenerateExpression(CsMarshalCallableBase param)
        {
            if (param.IsInterfaceArray)
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
                        return GenerateNullCheckIfNeeded(
                            param,
                            GetMarshalStorageLocation(param),
                            CastExpression(
                                PointerType(
                                    PredefinedType(
                                        Token(SyntaxKind.VoidKeyword))),
                                LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    Literal(0))));
                    }
                    else
                    {
                        return PrefixUnaryExpression(SyntaxKind.AddressOfExpression, GetMarshalStorageLocation(param));
                    }
                }
                else if (param.IsArray)
                {
                    if (param.HasNativeValueType)
                    {
                        return GenerateNullCheckIfNeeded(
                            param,
                            IdentifierName(param.IntermediateMarshalName),
                            CastExpression(
                                PointerType(
                                    PredefinedType(
                                        Token(SyntaxKind.VoidKeyword))),
                                LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    Literal(0))));
                    }
                    return GetMarshalStorageLocation(param);
                }
                else if (param.IsFixed && !param.HasNativeValueType)
                {
                    return param.UsedAsReturn
                        ? PrefixUnaryExpression(SyntaxKind.AddressOfExpression, IdentifierName(param.Name))
                        : GetMarshalStorageLocation(param);
                }
                else if (param.HasNativeValueType || param.IsBoolToInt)
                {
                    return PrefixUnaryExpression(SyntaxKind.AddressOfExpression, GetMarshalStorageLocation(param));
                }
                else if (param.IsString)
                {
                    return PrefixUnaryExpression(SyntaxKind.AddressOfExpression, GetMarshalStorageLocation(param));
                }
                else if (param.IsValueType)
                {
                    if (!param.MappedToDifferentPublicType)
                    {
                        return PrefixUnaryExpression(SyntaxKind.AddressOfExpression, IdentifierName(param.Name));
                    }
                    else
                    {
                        return PrefixUnaryExpression(SyntaxKind.AddressOfExpression, GetMarshalStorageLocation(param));
                    }
                }
                else
                {
                    return GetMarshalStorageLocation(param);
                }
            }
            if (param.PassedByNullableInstance)
            {
                return GenerateNullCheckIfNeeded(
                    param,
                    PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                        GetMarshalStorageLocation(param)),
                        CastExpression(
                                PointerType(
                                    PredefinedType(
                                        Token(SyntaxKind.VoidKeyword))),
                                LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    Literal(0)))
                        );
            }
            if (param.RefInPassedByValue)
            {
                return PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                    param.HasNativeValueType ? GetMarshalStorageLocation(param) : IdentifierName(param.Name));
            }
            if (!param.IsFixed && param.PublicType is CsEnum csEnum && !param.IsArray)
            {
                return CheckedExpression(
                    SyntaxKind.UncheckedExpression,
                    CastExpression(
                        ParseTypeName(csEnum.UnderlyingType?.Type.FullName ?? "int"),
                        IdentifierName(param.Name)));
            }
            
            if (param.IsString)
            {
                return CastExpression(
                    PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                    GetMarshalStorageLocation(param));
            }
            if (param.PublicType is CsInterface
                && param.IsIn
                && !param.IsArray)
            {
                return CastExpression(
                    PointerType(
                        PredefinedType(
                            Token(SyntaxKind.VoidKeyword))),
                    GetMarshalStorageLocation(param));
            }
            if (param.IsArray)
            {
                if (param.HasNativeValueType || param.IsBoolToInt)
                {
                    return IdentifierName(param.IntermediateMarshalName);
                }
                else if (param.IsValueType && param.IsOptional)
                {
                    return GetMarshalStorageLocation(param);
                }
            }
            if (param.IsBoolToInt)
            {
                return ParenthesizedExpression(
                    ConditionalExpression(IdentifierName(param.Name),
                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)),
                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))));
            }
            if (param.IsFixed && !param.HasNativeValueType)
            {
                return GetMarshalStorageLocation(param);
            }
            if (param.PublicType is CsFundamentalType fundamental && fundamental.Type == typeof(IntPtr) && !param.IsArray)
            {
                return CastExpression(
                    PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                    IdentifierName(param.Name));
            }
            if (param.HasNativeValueType)
            {
                return param.IsIn ?
                    GetMarshalStorageLocation(param)
                    : PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                        GetMarshalStorageLocation(param));
            }
            if (param.PublicType.QualifiedName == globalNamespace.GetTypeName(WellKnownName.PointerSize))
            {
                return CastExpression(
                    PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                    IdentifierName(param.Name));
            }
            return IdentifierName(param.Name);
        }
    }
}
