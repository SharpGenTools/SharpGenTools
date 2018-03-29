using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace SharpGen.Generator
{
    class ParameterCodeGenerator : ICodeGenerator<CsParameter, ParameterSyntax>
    {
        public ParameterSyntax GenerateCode(CsParameter csElement)
        {
            var param = Parameter(Identifier(csElement.Name));
            if (!csElement.IsFastOut && csElement.Attribute == CsParameterAttribute.Out
                && (!csElement.IsArray || csElement.PublicType.Name == "System.String"))
            {
                param = param.AddModifiers(Token(SyntaxKind.OutKeyword));
            }
            else if (
                (csElement.Attribute == CsParameterAttribute.Ref
                    || csElement.Attribute == CsParameterAttribute.RefIn)
                && !csElement.IsArray)
            {
                if (!(csElement.IsRefInValueTypeOptional || csElement.IsRefInValueTypeByValue)
                    && !csElement.IsStructClass)
                    param = param.AddModifiers(Token(SyntaxKind.RefKeyword));
            }
            else if (csElement.HasParams && csElement.IsArray)
            {
                param = param.AddModifiers(Token(SyntaxKind.ParamsKeyword));
            }

            var type = ParseTypeName(csElement.PublicType.QualifiedName);

            if (csElement.IsNullableStruct)
            {
                type = NullableType(type);
            }

            if (csElement.IsArray && csElement.PublicType.Name != "System.String" && !csElement.IsInterfaceArray)
            {
                type = ArrayType(type, SingletonList(ArrayRankSpecifier()));
            }

            return param.WithType(type);
        }
    }
}
