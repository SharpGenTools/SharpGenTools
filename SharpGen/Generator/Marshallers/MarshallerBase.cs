using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    enum StructMarshalMethod
    {
        From,
        To,
        Free
    }

    class MarshallerBase
    {
        protected readonly GlobalNamespaceProvider globalNamespace;

        public MarshallerBase(GlobalNamespaceProvider globalNamespace)
        {
            this.globalNamespace = globalNamespace;
        }

        protected TypeSyntax IntPtrType { get; } = ParseTypeName("System.IntPtr");

        protected StatementSyntax GenerateNullCheckIfNeeded(CsMarshalBase marshallable, StatementSyntax statement)
        {
            if (marshallable.IsOptional && (marshallable.IsArray || marshallable.IsInterface || marshallable.IsNullableStruct || marshallable.IsStructClass))
            {
                return IfStatement(
                                BinaryExpression(SyntaxKind.NotEqualsExpression,
                                    IdentifierName(marshallable.Name),
                                    LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                statement);
            }
            return statement;
        }

        protected ExpressionSyntax GenerateNullCheckIfNeeded(CsMarshalBase marshallable, ExpressionSyntax expression, ExpressionSyntax nullAlternative)
        {
            if (marshallable.IsOptional && (marshallable.IsArray || marshallable.IsInterface || marshallable.IsNullableStruct || marshallable.IsStructClass))
            {
                return ConditionalExpression(
                    BinaryExpression(SyntaxKind.EqualsExpression,
                        IdentifierName(marshallable.Name),
                        LiteralExpression(SyntaxKind.NullLiteralExpression)),
                        nullAlternative,
                        expression);
            }
            return expression;
        }

        protected StatementSyntax LoopThroughArrayParameter(
            CsMarshalBase marshallable,
            Func<ExpressionSyntax, ExpressionSyntax, StatementSyntax> loopBodyFactory,
            string variableName = "i")
        {
            var element = ElementAccessExpression(
                                                IdentifierName(marshallable.Name),
                                                BracketedArgumentList(
                                                    SingletonSeparatedList(
                                                        Argument(
                                                            IdentifierName(variableName)))));
            var nativeElement = ElementAccessExpression(
                                                ParenthesizedExpression(GetMarshalStorageLocation(marshallable)),
                                                BracketedArgumentList(
                                                    SingletonSeparatedList(
                                                        Argument(
                                                            IdentifierName(variableName)))));

            return GenerateNullCheckIfNeeded(marshallable,
                ForStatement(loopBodyFactory(element, nativeElement))
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
                            IdentifierName(marshallable.Name),
                            IdentifierName("Length"))))
                .WithIncrementors(
                    SingletonSeparatedList<ExpressionSyntax>(
                        PrefixUnaryExpression(
                            SyntaxKind.PreIncrementExpression,
                            IdentifierName(variableName)))));
        }

        protected StatementSyntax CreateMarshalStructStatement(
            CsMarshalBase marshallable,
            StructMarshalMethod marshalMethod,
            ExpressionSyntax publicElementExpr,
            ExpressionSyntax marshalElementExpr)
        {
            var statements = new List<StatementSyntax>();

            if (marshallable.IsStructClass && marshalMethod == StructMarshalMethod.From)
            {
                statements.Add(ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        publicElementExpr,
                        ObjectCreationExpression(ParseTypeName(marshallable.PublicType.QualifiedName))
                            .WithArgumentList(ArgumentList()))));
            }

            if (marshallable.IsStaticMarshal)
            {
                statements.Add(GenerateNullCheckIfNeeded(marshallable,
                    ExpressionStatement(InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            ParseTypeName(marshallable.PublicType.QualifiedName),
                            IdentifierName($"__Marshal{marshalMethod}")),
                        ArgumentList(
                            SeparatedList(
                                new[]
                                {
                                    Argument(publicElementExpr)
                                        .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                                    Argument(marshalElementExpr)
                                        .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword))
                                }))))));
            }
            else
            {
                statements.Add(GenerateNullCheckIfNeeded(marshallable,
                    ExpressionStatement(InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            publicElementExpr,
                            IdentifierName($"__Marshal{marshalMethod}")),
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(marshalElementExpr)
                                    .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword))))))));
            }

            return statements.Count == 1 ? statements[0] : Block(statements);
        }

        protected ExpressionStatementSyntax CreateMarshalCustomNewStatement(CsMarshalBase csElement, ExpressionSyntax marshalElement)
        {
            return ExpressionStatement(
                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    marshalElement,
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            ParseTypeName(csElement.PublicType.QualifiedName),
                            IdentifierName("__NewNative")))
                    .WithArgumentList(ArgumentList())));
        }

        protected StatementSyntax GenerateCopyMemory(CsMarshalBase marshallable, bool copyFromNative, bool useIntermediate = false)
        {
            return FixedStatement(
                VariableDeclaration(
                    PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                    SeparatedList(
                        new[]
                        {
                            VariableDeclarator(copyFromNative ? "__to": "__from")
                                .WithInitializer(EqualsValueClause(
                                    PrefixUnaryExpression(
                                        SyntaxKind.AddressOfExpression,
                                        ElementAccessExpression(
                                            useIntermediate ?
                                                IdentifierName(marshallable.IntermediateMarshalName)
                                                : IdentifierName(marshallable.Name)
                                        )
                                        .WithArgumentList(
                                            BracketedArgumentList(
                                                SingletonSeparatedList(
                                                    Argument(
                                                        LiteralExpression(
                                                            SyntaxKind.NumericLiteralExpression,
                                                            Literal(0))))))))),
                            VariableDeclarator(copyFromNative ? "__from" : "__to")
                                .WithInitializer(EqualsValueClause(
                                    PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                    GetMarshalStorageLocation(marshallable))))
                        })
                    ),
                GenerateCopyMemoryInvocation(
                    BinaryExpression(SyntaxKind.MultiplyExpression,
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(marshallable.ArrayDimensionValue)),
                    SizeOfExpression(ParseTypeName(marshallable.MarshalType.QualifiedName))
                )));
        }

        protected ExpressionStatementSyntax GenerateCopyMemoryInvocation(ExpressionSyntax numBytesExpression)
        {
            return ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        globalNamespace.GetTypeNameSyntax(WellKnownName.MemoryHelpers),
                                        IdentifierName("CopyMemory")),
                                    ArgumentList(
                                        SeparatedList(
                                            new[]
                                            {
                                    Argument(CastExpression(ParseTypeName("System.IntPtr"), IdentifierName("__to"))),
                                    Argument(CastExpression(ParseTypeName("System.IntPtr"), IdentifierName("__from"))),
                                    Argument(numBytesExpression)
                                            }
                                        ))));
        }

        internal static SyntaxToken GetMarshalStorageLocationIdentifier(CsMarshalBase marshallable)
        {
            switch (marshallable)
            {
                case CsParameter _:
                    return Identifier($"{marshallable.Name}_");
                case CsField _:
                    throw new ArgumentException("Marshal storage location for a field cannot be represented by a token.", nameof(marshallable));
                case CsReturnValue returnValue:
                    return Identifier(returnValue.MarshalStorageLocation);
                default:
                    throw new ArgumentException(nameof(marshallable));
            }
        }

        protected ExpressionSyntax GetMarshalStorageLocation(CsMarshalBase marshallable)
        {
            switch (marshallable)
            {
                case CsParameter _:
                case CsReturnValue _:
                    return IdentifierName(GetMarshalStorageLocationIdentifier(marshallable));
                case CsField _:
                    return MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("@ref"),
                        IdentifierName(marshallable.Name));
                default:
                    throw new ArgumentException(nameof(marshallable));
            }
        }

        protected StatementSyntax MarshalInterfaceInstanceFromNative(CsMarshalBase csElement, ExpressionSyntax publicElement, ExpressionSyntax marshalElement)
        {
            var interfaceType = (CsInterface)csElement.PublicType;

            if (csElement.IsFastOut)
            {
                return ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                ParenthesizedExpression(publicElement),
                                IdentifierName("NativePointer")),
                            marshalElement));
            }

            return IfStatement(
                    BinaryExpression(SyntaxKind.NotEqualsExpression,
                        marshalElement,
                        ParseExpression("System.IntPtr.Zero")),
                    ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        publicElement,
                        ObjectCreationExpression(ParseTypeName(interfaceType.GetNativeImplementationOrThis().QualifiedName))
                        .WithArgumentList(
                            ArgumentList(SingletonSeparatedList(
                                Argument(marshalElement)))))),
                    ElseClause(
                        ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            publicElement,
                            LiteralExpression(SyntaxKind.NullLiteralExpression,
                                Token(SyntaxKind.NullKeyword))))));
        }

        protected ExpressionStatementSyntax MarshalInterfaceInstanceToNative(CsMarshalBase csElement, ExpressionSyntax publicElement, ExpressionSyntax marshalElement)
        {
            return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    marshalElement,
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            globalNamespace.GetTypeNameSyntax(WellKnownName.CppObject),
                            GenericName(
                                Identifier("ToCallbackPtr"))
                            .WithTypeArgumentList(
                                TypeArgumentList(
                                    SingletonSeparatedList<TypeSyntax>(
                                        IdentifierName(csElement.PublicType.QualifiedName))))),
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    publicElement))))));
        }

        protected StatementSyntax NotImplemented(string message)
        {
            return ThrowStatement(
                ObjectCreationExpression(
                    ParseTypeName("System.NotImplementedException"))
                .WithArgumentList(
                    ArgumentList(
                        message == null ? default
                        : SingletonSeparatedList(
                            Argument(
                                LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    Literal(message)))))));
        }

        protected StatementSyntax NotSupported(string message)
        {
            return ThrowStatement(
                ObjectCreationExpression(
                    ParseTypeName("System.NotSupportedException"))
                .WithArgumentList(
                    ArgumentList(
                        message == null ? default
                        : SingletonSeparatedList(
                            Argument(
                                LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    Literal(message)))))));
        }

        protected ArgumentSyntax GenerateManagedValueTypeArgument(CsParameter csElement)
        {
            var arg = Argument(IdentifierName(csElement.Name));

            if (csElement.IsOut)
            {
                return arg.WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword));
            }
            else if (csElement.PassedByManagedReference)
            {
                return arg.WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword));
            }

            return arg;
        }

        protected ParameterSyntax GenerateManagedValueTypeParameter(CsParameter csElement)
        {
            var param = Parameter(Identifier(csElement.Name));
            if (csElement.IsOut)
            {
                param = param.AddModifiers(Token(SyntaxKind.OutKeyword));
            }
            else if (csElement.PassedByManagedReference)
            {
                param = param.AddModifiers(Token(SyntaxKind.RefKeyword));
            }

            var type = ParseTypeName(csElement.PublicType.QualifiedName);

            if (csElement.IsNullableStruct)
            {
                type = NullableType(type);
            }

            return param.WithType(type);
        }

        protected ParameterSyntax GenerateManagedArrayParameter(CsParameter csElement)
        {
            var param = Parameter(Identifier(csElement.Name))
                .WithType(ArrayType(ParseTypeName(csElement.PublicType.QualifiedName), SingletonList(ArrayRankSpecifier())));

            if (csElement.HasParams)
            {
                param = param.AddModifiers(Token(SyntaxKind.ParamsKeyword));
            }

            return param;
        }

        protected StatementSyntax GenerateArrayNativeToManagedExtendedProlog(CsMarshalCallableBase csElement)
        {
            // e.g. Function(int[] buffer, int length)
            // callable is Function
            // csElement is buffer
            // lengthParam is length

            var callable = (CsCallable) csElement.Parent;

            bool MatchPredicate(CsParameter param)
            {
                var relations = param.Relations;

                if (relations is null) return false;

                return relations.OfType<LengthRelation>()
                    .Any(relation => relation.Identifier == csElement.CppElementName);
            }

            var lengthParam = callable.Parameters
                .Where(MatchPredicate)
                .ToArray();

            if (lengthParam.Length == 0)
            {
                return NotSupported("Cannot marshal a native array to a managed array when length is not specified");
            }
            
            if (lengthParam.Length > 1)
            {
                return NotSupported("Cannot marshal a native array to a managed array when length is specified multiple times");
            }

            var marshaller = new LengthRelationMarshaller(globalNamespace);
            return marshaller.GenerateNativeToManaged(csElement, lengthParam[0]);
        }
        
        protected StatementSyntax GenerateGCKeepAlive(CsMarshalBase csElement)
        {
            return ExpressionStatement(
                InvocationExpression(
                    ParseName("System.GC.KeepAlive"),
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(IdentifierName(csElement.Name))
                        )
                    )
                )
            );
        }
    }
}
