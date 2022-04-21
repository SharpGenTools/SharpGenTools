using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Generator.Marshallers;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator;

internal sealed class ReverseCallablePrologCodeGenerator : StatementPlatformMultiCodeGeneratorBase<CsCallable>
{
    public override IEnumerable<PlatformDetectionType> GetPlatforms(CsCallable csElement) => csElement.InteropSignatures.Keys;

    public override IEnumerable<StatementSyntax> GenerateCode(CsCallable csElement, PlatformDetectionType platform)
    {
        var interopSig = csElement.InteropSignatures[platform];

        if (!interopSig.ForcedReturnBufferSig && csElement.HasReturnTypeValue)
        {
            foreach (var statement in GenerateProlog(csElement.ReturnValue, null, null))
            {
                yield return statement;
            }
        }

        foreach (var signatureParameter in interopSig.ParameterTypes)
        {
            var publicParameter = signatureParameter.Item;
            var nativeParameter = IdentifierName(signatureParameter.Name);
            var interopTypeSyntax = signatureParameter.InteropTypeSyntax;
            var builder = GetPrologBuilder(publicParameter);

            foreach (var statement in builder(publicParameter, nativeParameter, interopTypeSyntax))
                yield return statement;
        }
    }

    private readonly Lazy<PrologGenerationDelegate> _nativeByRefPrologDelegate;
    private readonly Lazy<PrologGenerationDelegate> _prologDelegate;

    private PrologGenerationDelegate GetPrologBuilder(CsMarshalCallableBase publicParameter) =>
        publicParameter.PassedByNativeReference && !publicParameter.IsArray
            ? _nativeByRefPrologDelegate.Value
            : _prologDelegate.Value;

    private delegate IEnumerable<StatementSyntax> PrologGenerationDelegate(
        CsMarshalCallableBase publicElement, ExpressionSyntax nativeParameter, TypeSyntax nativeParameterType
    );

    private IEnumerable<StatementSyntax> GenerateProlog(CsMarshalCallableBase publicElement,
                                                        ExpressionSyntax nativeParameter,
                                                        TypeSyntax nativeParameterType)
    {
        ExpressionSyntax CastToPublicType(TypeSyntax targetType, ExpressionSyntax expression) =>
            targetType.IsEquivalentTo(nativeParameterType)
                ? expression
                : GeneratorHelpers.CastExpression(targetType, expression);

        var marshaller = GetMarshaller(publicElement);
        var publicType = GetPublicType(publicElement);

        var generatesMarshalVariable = marshaller.GeneratesMarshalVariable(publicElement);

        var publicTypeVariableValue = nativeParameter != null && !generatesMarshalVariable
                                          ? CastToPublicType(publicType, nativeParameter)
                                          : DefaultLiteral;

        yield return LocalDeclarationStatement(
            VariableDeclaration(
                publicType,
                SingletonSeparatedList(
                    VariableDeclarator(Identifier(publicElement.Name))
                       .WithInitializer(EqualsValueClause(publicTypeVariableValue))
                )
            )
        );

        if (generatesMarshalVariable)
        {
            var marshalTypeSyntax = marshaller.GetMarshalTypeSyntax(publicElement);

            var initializerExpression = nativeParameter != null
                                            ? CastToPublicType(marshalTypeSyntax, nativeParameter)
                                            : DefaultLiteral;

            yield return LocalDeclarationStatement(
                VariableDeclaration(
                    marshalTypeSyntax,
                    SingletonSeparatedList(
                        VariableDeclarator(
                            MarshallerBase.GetMarshalStorageLocationIdentifier(publicElement),
                            null,
                            EqualsValueClause(initializerExpression)
                        )
                    )
                )
            );
        }
    }

    internal static TypeSyntax GetPublicType(CsMarshalCallableBase publicElement)
    {
        var publicType = ParseTypeName(publicElement.PublicType.QualifiedName);
        return publicElement.IsArray
                   ? ArrayType(publicType, SingletonList(ArrayRankSpecifier()))
                   : publicType;
    }

    private IEnumerable<StatementSyntax> GenerateNativeByRefProlog(CsMarshalCallableBase publicElement,
                                                                   ExpressionSyntax nativeParameter,
                                                                   TypeSyntax nativeParameterType)
    {
        var marshaller = GetMarshaller(publicElement);
        var marshalTypeSyntax = marshaller.GetMarshalTypeSyntax(publicElement);
        var publicType = ParseTypeName(publicElement.PublicType.QualifiedName);
        var generatesMarshalVariable = marshaller.GeneratesMarshalVariable(publicElement);
        ExpressionSyntax publicDefaultValue = publicElement.UsedAsReturn ? default : DefaultLiteral;

        var refToNativeClause = GenerateAsRefInitializer(publicElement, nativeParameter, marshalTypeSyntax);

        TypeSyntax publicVariableType, marshalVariableType;
        ExpressionSyntax publicVariableInitializer, marshalVariableInitializer;

        if (publicElement is CsParameter {IsOptional: true, IsLocalManagedReference: true} parameter)
        {
            Debug.Assert(marshaller is RefWrapperMarshaller);

            var refVariableDeclaration = LocalDeclarationStatement(
                VariableDeclaration(
                    RefType(marshalTypeSyntax),
                    SingletonSeparatedList(
                        VariableDeclarator(
                            MarshallerBase.GetRefLocationIdentifier(publicElement),
                            default, EqualsValueClause(refToNativeClause)
                        )
                    )
                )
            );

            publicVariableType = publicType;
            marshalVariableType = marshalTypeSyntax;
            publicVariableInitializer = publicDefaultValue;
            marshalVariableInitializer = DefaultLiteral;

            if (generatesMarshalVariable && parameter is {IsRef: true})
            {
                Logger.Error(
                    null, "Optional ref parameter [{0}] that requires generating marshal variable is unsupported.",
                    parameter.QualifiedName
                );
            }
            else
            {
                Debug.Assert(!generatesMarshalVariable || parameter is {IsOut: true});
            }

            yield return refVariableDeclaration;
        }
        else
        {
            Debug.Assert(marshaller is not RefWrapperMarshaller);

            marshalVariableInitializer = refToNativeClause;

            if (publicElement is {IsLocalManagedReference: true})
            {
                Debug.Assert(publicElement is CsReturnValue or CsParameter {IsOptional: false});

                marshalVariableType = RefType(marshalTypeSyntax);
                publicVariableType = generatesMarshalVariable
                                         ? publicType
                                         : RefType(publicType);
                publicVariableInitializer = generatesMarshalVariable
                                                ? publicDefaultValue
                                                : refToNativeClause;

            }
            else
            {
                Debug.Assert(publicElement is CsParameter {IsRefIn: true});

                var isNullable = publicElement is CsParameter {PassedByNullableInstance: true};

                marshalVariableType = marshalTypeSyntax;
                publicVariableType = isNullable ? NullableType(publicType) : publicType;
                publicVariableInitializer = generatesMarshalVariable
                                                ? isNullable
                                                      ? ConditionalExpression(
                                                          BinaryExpression(
                                                              SyntaxKind.NotEqualsExpression,
                                                              nativeParameter, DefaultLiteral
                                                          ),
                                                          ImplicitObjectCreationExpression(),
                                                          NullLiteral
                                                      )
                                                      : publicDefaultValue
                                                : refToNativeClause;
            }
        }

        if (generatesMarshalVariable)
        {
            yield return LocalDeclarationStatement(
                VariableDeclaration(
                    marshalVariableType,
                    SingletonSeparatedList(
                        VariableDeclarator(
                            MarshallerBase.GetMarshalStorageLocationIdentifier(publicElement),
                            default, EqualsValueClause(marshalVariableInitializer)
                        )
                    )
                )
            );
        }

        yield return LocalDeclarationStatement(
            VariableDeclaration(
                publicVariableType,
                SingletonSeparatedList(
                    VariableDeclarator(
                        Identifier(publicElement.Name), default,
                        publicVariableInitializer != default
                            ? EqualsValueClause(publicVariableInitializer)
                            : default
                    )
                )
            )
        );
    }

    private ExpressionSyntax GenerateAsRefInitializer(CsMarshalCallableBase publicElement,
                                                      ExpressionSyntax nativeParameter,
                                                      TypeSyntax marshalTypeSyntax)
    {
        var refToNativeExpression = InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                GlobalNamespace.GetTypeNameSyntax(BuiltinType.Unsafe),
                GenericName(
                    Identifier(nameof(Unsafe.AsRef)),
                    TypeArgumentList(SingletonSeparatedList(marshalTypeSyntax))
                )
            ),
            ArgumentList(SingletonSeparatedList(Argument(nativeParameter)))
        );

        ExpressionSyntax refToNativeClauseExpression;

        if (publicElement.IsLocalManagedReference)
        {
            Debug.Assert(
                publicElement is CsReturnValue or CsParameter
                {
                    Attribute: CsParameterAttribute.Ref or CsParameterAttribute.Out
                }
            );

            refToNativeClauseExpression = RefExpression(refToNativeExpression);
        }
        else
        {
            Debug.Assert(publicElement is CsParameter {IsRefIn: true});

            if (publicElement is CsParameter {IsOptional: true})
            {
                refToNativeClauseExpression = ConditionalExpression(
                    BinaryExpression(SyntaxKind.NotEqualsExpression, nativeParameter, DefaultLiteral),
                    refToNativeExpression,
                    DefaultLiteral
                );
            }
            else
            {
                refToNativeClauseExpression = refToNativeExpression;
            }
        }

        return refToNativeClauseExpression;
    }

    public ReverseCallablePrologCodeGenerator(Ioc ioc) : base(ioc)
    {
        _nativeByRefPrologDelegate = new(() => GenerateNativeByRefProlog, LazyThreadSafetyMode.None);
        _prologDelegate = new(() => GenerateProlog, LazyThreadSafetyMode.None);
    }
}