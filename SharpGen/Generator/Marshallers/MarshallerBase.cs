using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal enum StructMarshalMethod
    {
        From,
        To,
        Free
    }

    internal abstract class MarshallerBase
    {
        protected readonly GlobalNamespaceProvider GlobalNamespace;

        protected MarshallerBase(GlobalNamespaceProvider globalNamespace)
        {
            GlobalNamespace = globalNamespace;
        }

        protected static TypeSyntax IntPtrType { get; } = ParseTypeName("System.IntPtr");

        protected static MemberAccessExpressionSyntax IntPtrZero { get; } =
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IntPtrType,
                IdentifierName(nameof(IntPtr.Zero))
            );

        protected static TypeSyntax VoidPtrType { get; } = PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword)));

        private static bool IsNullable(CsMarshalBase marshallable) =>
            marshallable.IsOptional && (marshallable.IsArray || marshallable.IsInterface ||
                                        marshallable.IsNullableStruct || marshallable.IsStructClass);

        protected static StatementSyntax GenerateNullCheckIfNeeded(CsMarshalBase marshallable,
                                                                   StatementSyntax statement) =>
            IsNullable(marshallable)
                ? IfStatement(
                    BinaryExpression(
                        SyntaxKind.NotEqualsExpression,
                        IdentifierName(marshallable.Name),
                        LiteralExpression(SyntaxKind.NullLiteralExpression)
                    ),
                    statement
                )
                : statement;

        protected static ExpressionSyntax GenerateNullCheckIfNeeded(CsMarshalBase marshallable,
                                                                    ExpressionSyntax expression,
                                                                    ExpressionSyntax nullAlternative) =>
            IsNullable(marshallable)
                ? ConditionalExpression(
                    BinaryExpression(
                        SyntaxKind.EqualsExpression,
                        IdentifierName(marshallable.Name),
                        LiteralExpression(SyntaxKind.NullLiteralExpression)
                    ),
                    nullAlternative,
                    expression
                )
                : expression;

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

        protected static ExpressionStatementSyntax CreateMarshalCustomNewStatement(CsMarshalBase csElement, ExpressionSyntax marshalElement)
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
                    VoidPtrType,
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
                                        GlobalNamespace.GetTypeNameSyntax(WellKnownName.MemoryHelpers),
                                        IdentifierName("CopyMemory")),
                                    ArgumentList(
                                        SeparatedList(
                                            new[]
                                            {
                                    Argument(CastExpression(IntPtrType, IdentifierName("__to"))),
                                    Argument(CastExpression(IntPtrType, IdentifierName("__from"))),
                                    Argument(numBytesExpression)
                                            }
                                        ))));
        }

        protected internal static SyntaxToken GetMarshalStorageLocationIdentifier(CsMarshalBase marshallable) =>
            marshallable switch
            {
                CsParameter => Identifier($"{marshallable.Name}_"),
                CsField => throw new ArgumentException(
                               "Marshal storage location for a field cannot be represented by a token.",
                               nameof(marshallable)
                           ),
                CsReturnValue => Identifier(CsReturnValue.MarshalStorageLocation),
                _ => throw new ArgumentException(nameof(marshallable))
            };

        protected internal static ExpressionSyntax GetMarshalStorageLocation(CsMarshalBase marshallable) =>
            marshallable switch
            {
                CsParameter => IdentifierName(GetMarshalStorageLocationIdentifier(marshallable)),
                CsReturnValue => IdentifierName(GetMarshalStorageLocationIdentifier(marshallable)),
                CsField => MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression, IdentifierName("@ref"),
                    IdentifierName(marshallable.Name)
                ),
                _ => throw new ArgumentException(nameof(marshallable))
            };

        protected static StatementSyntax MarshalInterfaceInstanceFromNative(CsMarshalBase csElement,
                                                                            ExpressionSyntax publicElement,
                                                                            ExpressionSyntax marshalElement)
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

            return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    publicElement,
                    ConditionalExpression(
                        BinaryExpression(SyntaxKind.NotEqualsExpression, marshalElement, IntPtrZero),
                        ObjectCreationExpression(
                                ParseTypeName(interfaceType.GetNativeImplementationOrThis().QualifiedName)
                            )
                           .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(marshalElement)))),
                        LiteralExpression(SyntaxKind.NullLiteralExpression, Token(SyntaxKind.NullKeyword))
                    )
                )
            );
        }

        protected ExpressionStatementSyntax MarshalInterfaceInstanceToNative(CsMarshalBase csElement,
                                                                             ExpressionSyntax publicElement,
                                                                             ExpressionSyntax marshalElement) =>
            ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    marshalElement,
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            GlobalNamespace.GetTypeNameSyntax(WellKnownName.CppObject),
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

        private static ThrowStatementSyntax ThrowException(string exceptionName, string message) =>
            ThrowStatement(
                ObjectCreationExpression(
                    ParseTypeName(exceptionName),
                    ArgumentList(
                        message == null
                            ? default
                            : SingletonSeparatedList(
                                Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(message)))
                            )
                    ),
                    null
                )
            );

        protected static StatementSyntax NotImplemented(string message) =>
            ThrowException("System.NotImplementedException", message);

        protected static StatementSyntax NotSupported(string message) =>
            ThrowException("System.NotSupportedException", message);

        protected static ArgumentSyntax GenerateManagedValueTypeArgument(CsParameter csElement)
        {
            var arg = Argument(IdentifierName(csElement.Name));

            if (csElement.IsOut)
            {
                return arg.WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword));
            }

            if (csElement.PassedByManagedReference)
            {
                return arg.WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword));
            }

            return arg;
        }

        protected static ParameterSyntax GenerateManagedValueTypeParameter(CsParameter csElement)
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

        protected static ParameterSyntax GenerateManagedArrayParameter(CsParameter csElement)
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

            return lengthParam.Length switch
            {
                0 => NotSupported("Cannot marshal a native array to a managed array when length is not specified"),
                > 1 => NotSupported(
                    "Cannot marshal a native array to a managed array when length is specified multiple times"
                ),
                _ => new LengthRelationMarshaller(GlobalNamespace).GenerateNativeToManaged(csElement, lengthParam[0])
            };
        }

        protected static StatementSyntax GenerateGCKeepAlive(CsMarshalBase csElement) =>
            ExpressionStatement(
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
