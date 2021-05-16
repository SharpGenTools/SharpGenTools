using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Logging;
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

    internal abstract partial class MarshallerBase
    {
        protected static readonly SyntaxToken PtrIdentifier = Identifier("__ptr");
        protected static readonly IdentifierNameSyntax PtrIdentifierName = IdentifierName(PtrIdentifier);
        protected static readonly SyntaxToken LengthIdentifier = Identifier("__length");
        protected static readonly IdentifierNameSyntax LengthIdentifierName = IdentifierName(LengthIdentifier);

        protected static readonly LiteralExpressionSyntax DefaultLiteral = LiteralExpression(
            SyntaxKind.DefaultLiteralExpression
        );

        protected static readonly LiteralExpressionSyntax NullLiteral = LiteralExpression(
            SyntaxKind.NullLiteralExpression
        );

        private readonly Ioc ioc;
        protected GlobalNamespaceProvider GlobalNamespace => ioc.GlobalNamespace;
        protected Logger Logger => ioc.Logger;

        protected MarshallerBase(Ioc ioc)
        {
            this.ioc = ioc ?? throw new ArgumentNullException(nameof(ioc));
        }

        protected static TypeSyntax IntPtrType => GeneratorHelpers.IntPtrType;
        protected static MemberAccessExpressionSyntax IntPtrZero => GeneratorHelpers.IntPtrZero;
        protected static TypeSyntax VoidPtrType => GeneratorHelpers.VoidPtrType;
        protected static LiteralExpressionSyntax ZeroLiteral => GeneratorHelpers.ZeroLiteral;

        private static bool IsNullable(CsMarshalBase marshallable) => marshallable is CsParameter {IsNullable: true};

        protected static StatementSyntax GenerateNullCheckIfNeeded(CsMarshalBase marshallable,
                                                                   StatementSyntax statement) =>
            IsNullable(marshallable)
                ? IfStatement(
                    BinaryExpression(SyntaxKind.NotEqualsExpression, IdentifierName(marshallable.Name), NullLiteral),
                    statement
                )
                : statement;

        protected static ExpressionSyntax GenerateNullCheckIfNeeded(CsMarshalBase marshallable,
                                                                    ExpressionSyntax expression,
                                                                    ExpressionSyntax nullAlternative) =>
            IsNullable(marshallable)
                ? ConditionalExpression(
                    BinaryExpression(SyntaxKind.EqualsExpression, IdentifierName(marshallable.Name), NullLiteral),
                    nullAlternative, expression
                )
                : expression;

        protected static StatementSyntax LoopThroughArrayParameter(
            CsMarshalBase marshallable,
            Func<ElementAccessExpressionSyntax, ElementAccessExpressionSyntax, StatementSyntax> loopBodyFactory,
            string variableName = "i")
        {
            var indexVariable = Identifier(variableName);
            var indexVariableName = IdentifierName(variableName);
            var arrayIdentifier = IdentifierName(marshallable.Name);

            var element = ElementAccessExpression(
                arrayIdentifier,
                BracketedArgumentList(SingletonSeparatedList(Argument(indexVariableName)))
            );
            var nativeElement = ElementAccessExpression(
                ParenthesizedExpression(GetMarshalStorageLocation(marshallable)),
                BracketedArgumentList(SingletonSeparatedList(Argument(indexVariableName)))
            );

            return GenerateNullCheckIfNeeded(
                marshallable,
                ForStatement(loopBodyFactory(element, nativeElement))
                   .WithDeclaration(
                        VariableDeclaration(
                            TypeInt32,
                            SeparatedList(
                                new[]
                                {
                                    VariableDeclarator(indexVariable, default, EqualsValueClause(ZeroLiteral)),
                                    VariableDeclarator(
                                        LengthIdentifier, default,
                                        EqualsValueClause(GeneratorHelpers.LengthExpression(arrayIdentifier))
                                    )
                                }
                            )))
                   .WithCondition(
                        BinaryExpression(SyntaxKind.LessThanExpression, indexVariableName, LengthIdentifierName)
                    )
                   .WithIncrementors(
                        SingletonSeparatedList<ExpressionSyntax>(
                            PrefixUnaryExpression(
                                SyntaxKind.PreIncrementExpression,
                                indexVariableName)))
            );
        }

        protected static StatementSyntax CreateMarshalStructStatement(
            CsMarshalBase marshallable,
            StructMarshalMethod marshalMethod,
            ExpressionSyntax publicElementExpr,
            ExpressionSyntax marshalElementExpr)
        {
            StatementSyntaxList statements = new();

            var marshalArgument = Argument(marshalElementExpr).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword));

            if (marshallable.PublicType is CsStruct {GenerateAsClass: true} structType &&
                marshalMethod == StructMarshalMethod.From)
            {
                var constructor = ObjectCreationExpression(ParseTypeName(structType.QualifiedName));
                var argumentList = !structType.HasCustomMarshal
                                       ? ArgumentList(SingletonSeparatedList(marshalArgument))
                                       : ArgumentList();

                statements.Add(
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            publicElementExpr, constructor.WithArgumentList(argumentList)
                        )
                    )
                );

                if (!structType.HasCustomMarshal)
                    return statements.ToStatement();
            }

            var methodName = IdentifierName($"__Marshal{marshalMethod}");

            var invocationExpression = marshallable.IsStaticMarshal
                                           ? InvocationExpression(
                                               MemberAccessExpression(
                                                   SyntaxKind.SimpleMemberAccessExpression,
                                                   ParseTypeName(marshallable.PublicType.QualifiedName), methodName
                                               ),
                                               ArgumentList(
                                                   SeparatedList(
                                                       new[]
                                                       {
                                                           Argument(publicElementExpr)
                                                              .WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                                                           marshalArgument
                                                       }
                                                   )
                                               )
                                           )
                                           : InvocationExpression(
                                               MemberAccessExpression(
                                                   SyntaxKind.SimpleMemberAccessExpression,
                                                   publicElementExpr, methodName
                                               ),
                                               ArgumentList(SingletonSeparatedList(marshalArgument))
                                           );

            statements.Add(GenerateNullCheckIfNeeded(marshallable, ExpressionStatement(invocationExpression)));

            return statements.ToStatement();
        }

        protected static StatementSyntax GenerateMarshalStructManagedToNative(CsMarshalBase csElement,
                                                                              ExpressionSyntax publicElement,
                                                                              ExpressionSyntax marshalElement)
        {
            var marshalTo = CreateMarshalStructStatement(
                csElement,
                StructMarshalMethod.To,
                publicElement,
                marshalElement
            );
            return ((CsStruct) csElement.PublicType).HasCustomNew
                       ? Block(
                           CreateMarshalCustomNewStatement(csElement, marshalElement),
                           marshalTo
                       )
                       : marshalTo;
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

        protected internal static SyntaxToken GetMarshalStorageLocationIdentifier(CsMarshalCallableBase marshallable) =>
            marshallable switch
            {
                CsParameter => Identifier($"{marshallable.Name}_"),
                CsReturnValue => Identifier(CsReturnValue.MarshalStorageLocation),
                _ => throw new ArgumentException(nameof(marshallable))
            };

        protected internal static SyntaxToken GetRefLocationIdentifier(CsMarshalCallableBase marshallable) =>
            marshallable switch
            {
                CsParameter => Identifier($"{marshallable.Name}_ref_"),
                CsReturnValue => throw new Exception("Return values as ref locals are not supported"),
                _ => throw new ArgumentException(nameof(marshallable))
            };

        protected internal static ExpressionSyntax GetMarshalStorageLocation(CsMarshalBase marshallable) =>
            marshallable switch
            {
                CsParameter parameter => IdentifierName(GetMarshalStorageLocationIdentifier(parameter)),
                CsReturnValue returnValue => IdentifierName(GetMarshalStorageLocationIdentifier(returnValue)),
                CsField => MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression, IdentifierName("@ref"),
                    IdentifierName(marshallable.Name)
                ),
                _ => throw new ArgumentException(nameof(marshallable))
            };

        protected static StatementSyntax MarshalInterfaceInstanceFromNative(CsMarshalBase csElement,
                                                                            ExpressionSyntax publicElement,
                                                                            ExpressionSyntax marshalElement) =>
            ExpressionStatement(
                csElement switch
                {
                    CsParameter {IsFast: true, IsOut: true} => AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            publicElement, IdentifierName("NativePointer")
                        ),
                        marshalElement
                    ),
                    _ => AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression, publicElement,
                        ConditionalExpression(
                            BinaryExpression(SyntaxKind.NotEqualsExpression, marshalElement, IntPtrZero),
                            ObjectCreationExpression(
                                    ParseTypeName(csElement.PublicType.GetNativeImplementationQualifiedName())
                                )
                               .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(marshalElement)))),
                            NullLiteral
                        )
                    )
                }
            );

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

            bool RelationPredicate(LengthRelation relation) => relation.Identifier == csElement.CppElementName;
            bool MatchPredicate(CsParameter param) => param.Relations.OfType<LengthRelation>().Any(RelationPredicate);

            var lengthParam = callable.Parameters.Where(MatchPredicate).ToArray();

            return lengthParam.Length switch
            {
                0 => NotSupported("Cannot marshal a native array [{0}] to a managed array when length is not specified"),
                > 1 => NotSupported(
                    "Cannot marshal a native array [{0}] to a managed array when length is specified multiple times"
                ),
                _ => LengthRelationMarshaller.GenerateNativeToManaged(csElement, lengthParam[0])
            };

            StatementSyntax NotSupported(string hint)
            {
                Logger.Error(LoggingCodes.InvalidLengthRelation, hint, csElement.QualifiedName);
                return null;
            }
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
