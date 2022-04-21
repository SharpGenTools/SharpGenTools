using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers;

internal sealed partial class ValueTypeArrayMarshaller : MarshallerBase, IMarshaller
{
    public bool CanMarshal(CsMarshalBase csElement) => csElement.IsValueType && csElement.IsArray &&
                                                       !csElement.MappedToDifferentPublicType;

    public ArgumentSyntax GenerateManagedArgument(CsParameter csElement) =>
        Argument(IdentifierName(csElement.Name));

    public ParameterSyntax GenerateManagedParameter(CsParameter csElement) =>
        GenerateManagedArrayParameter(csElement);

    public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame)
    {
        const ArrayCopyDirection direction = ArrayCopyDirection.ManagedToNative;

        return csElement switch
        {
            CsParameter {IsLocalManagedReference: true} parameter => GenerateCopyBlock(parameter, direction),
            CsField field => GenerateCopyMemory(field, direction),
            _ => null
        };
    }

    public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement) =>
        Enumerable.Empty<StatementSyntax>();

    public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement) =>
        Argument(GetMarshalStorageLocation(csElement));

    public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame) => null;

    public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame)
    {
        const ArrayCopyDirection direction = ArrayCopyDirection.NativeToManaged;

        return csElement switch
        {
            CsParameter {PassedByManagedReference: true} parameter => GenerateCopyBlock(parameter, direction),
            CsField field => GenerateCopyMemory(field, direction),
            _ => null
        };
    }

    public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement)
    {
        yield return GenerateArrayNativeToManagedExtendedProlog(csElement);
    }

    public FixedStatementSyntax GeneratePin(CsParameter csElement) => FixedStatement(
        VariableDeclaration(
            GetMarshalTypeSyntax(csElement),
            SingletonSeparatedList(
                VariableDeclarator(GetMarshalStorageLocationIdentifier(csElement)).WithInitializer(
                    EqualsValueClause(IdentifierName(csElement.Name))
                )
            )
        ),
        EmptyStatement()
    );

    public bool GeneratesMarshalVariable(CsMarshalCallableBase csElement) => true;

    public TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement) =>
        PointerType(ParseTypeName(csElement.PublicType.QualifiedName));

    public ValueTypeArrayMarshaller(Ioc ioc) : base(ioc)
    {
    }
}