using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers;

internal abstract class ArrayMarshallerBase : MarshallerBase, IMarshaller
{
    public ArgumentSyntax GenerateManagedArgument(CsParameter csElement) =>
        Argument(IdentifierName(csElement.Name));

    public ParameterSyntax GenerateManagedParameter(CsParameter csElement) =>
        GenerateManagedArrayParameter(csElement);

    public abstract StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame);

    public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement)
    {
        var identifier = GetMarshalStorageLocationIdentifier(csElement);
        var elementType = GetMarshalElementTypeSyntax(csElement);
        var spanTypeName = GlobalNamespace.GetGenericTypeNameSyntax(
            BuiltinType.Span,
            TypeArgumentList(SingletonSeparatedList(elementType))
        );

        ArrayTypeSyntax GetArrayType(ExpressionSyntax length) => ArrayType(
            elementType,
            SingletonList(ArrayRankSpecifier(SingletonSeparatedList(length)))
        );

        yield return LocalDeclarationStatement(
            VariableDeclaration(
                spanTypeName,
                SingletonSeparatedList(
                    VariableDeclarator(
                        identifier, default,
                        EqualsValueClause(StackAllocArrayCreationExpression(GetArrayType(ZeroLiteral)))
                    )
                )
            )
        );

        var variable = IdentifierName(identifier);
        var length = GeneratorHelpers.LengthExpression(IdentifierName(csElement.Name));

        var arrayType = GetArrayType(LengthIdentifierName);

        yield return GenerateNullCheckIfNeeded(
            csElement,
            Block(
                LocalDeclarationStatement(
                    VariableDeclaration(
                        TypeInt32,
                        SingletonSeparatedList(
                            VariableDeclarator(LengthIdentifier, default, EqualsValueClause(length))
                        )
                    )
                ),
                ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        variable,
                        ConditionalExpression(
                            BinaryExpression(
                                SyntaxKind.LessThanExpression,
                                BinaryExpression(
                                    SyntaxKind.MultiplyExpression,
                                    GeneratorHelpers.CastExpression(TypeUInt32, LengthIdentifierName),
                                    SizeOf(elementType)
                                ),
                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1024u))
                            ),
                            StackAllocArrayCreationExpression(arrayType),
                            ObjectCreationExpression(arrayType)
                        )
                    )
                )
            )
        );
    }

    public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement)
    {
        yield return GenerateArrayNativeToManagedExtendedProlog(csElement);
    }

    public abstract StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame);
    public abstract StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame);

    public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement) =>
        Argument(IdentifierName(csElement.IntermediateMarshalName));

    public FixedStatementSyntax GeneratePin(CsParameter csElement) => FixedStatement(
        VariableDeclaration(
            VoidPtrType,
            SingletonSeparatedList(
                VariableDeclarator(
                    Identifier(csElement.IntermediateMarshalName),
                    null,
                    EqualsValueClause(GetMarshalStorageLocation(csElement))
                )
            )
        ),
        EmptyStatement()
    );

    public abstract bool CanMarshal(CsMarshalBase csElement);

    public bool GeneratesMarshalVariable(CsMarshalCallableBase csElement) => true;

    protected abstract TypeSyntax GetMarshalElementTypeSyntax(CsMarshalBase csElement);

    public TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement) =>
        PointerType(GetMarshalElementTypeSyntax(csElement));

    protected ArrayMarshallerBase(Ioc ioc) : base(ioc)
    {
    }
}