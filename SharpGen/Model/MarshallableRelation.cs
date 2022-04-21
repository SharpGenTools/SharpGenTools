using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SharpGen.Model;

public abstract class MarshallableRelation
{
}

public sealed class StructSizeRelation : MarshallableRelation
{
    public override string ToString()
    {
        return "Size of enclosing structure";
    }
}

public sealed class LengthRelation : MarshallableRelation
{
    public string Identifier { get; set; }

    public override string ToString()
    {
        return $"Length of '{Identifier}'";
    }
}

public sealed class ConstantValueRelation : MarshallableRelation
{
    public ExpressionSyntax Value { get; set; }

    public override string ToString()
    {
        return $"Constant Value '{Value}'";
    }
}