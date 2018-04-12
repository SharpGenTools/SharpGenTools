using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.CSharp;
using SharpGen.Model;

namespace SharpGen.Generator
{
    class PinningCodeGenerator : MarshallingCodeGeneratorBase, ICodeGenerator<CsParameter, FixedStatementSyntax>
    {
        public PinningCodeGenerator(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public FixedStatementSyntax GenerateCode(CsParameter param)
        {
            if (param.IsArray && param.IsValueType)
            {
                if (param.HasNativeValueType)
                {
                    return FixedStatement(VariableDeclaration(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                        SingletonSeparatedList(
                            VariableDeclarator(param.IntermediateMarshalName).WithInitializer(EqualsValueClause(
                                GetMarshalStorageLocation(param)
                                )))), EmptyStatement());
                }
                else
                {
                    return FixedStatement(VariableDeclaration(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                        SingletonSeparatedList(
                            VariableDeclarator(GetMarshalStorageLocationIdentifier(param)).WithInitializer(EqualsValueClause(
                                IdentifierName(param.Name)
                                )))), EmptyStatement());
                }
            }
            else if (param.IsFixed && param.IsValueType && !param.HasNativeValueType && !param.IsUsedAsReturnType)
            {
                return FixedStatement(VariableDeclaration(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                    SingletonSeparatedList(
                        VariableDeclarator(GetMarshalStorageLocationIdentifier(param)).WithInitializer(EqualsValueClause(
                            PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                IdentifierName(param.Name))
                            )))), EmptyStatement());
            }
            else if (param.IsString && param.IsWideChar)
            {
                return FixedStatement(VariableDeclaration(PointerType(PredefinedType(Token(SyntaxKind.CharKeyword))),
                    SingletonSeparatedList(
                        VariableDeclarator(GetMarshalStorageLocationIdentifier(param)).WithInitializer(EqualsValueClause(
                            IdentifierName(param.Name)
                            )))), EmptyStatement());
            }
            return null;
        }
    }
}
