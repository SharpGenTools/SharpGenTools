namespace SharpGen.Model;

public struct ArraySpecification
{
    public ArraySpecification(uint dimension) => Dimension = dimension;

    public uint? Dimension { get; set; }
}