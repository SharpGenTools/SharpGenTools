namespace SharpGen.Platform.Clang.Abstractions
{
    public partial interface IOutputBuilder
    {
        bool IsTestOutput { get; }
        string Name { get; }
        string Extension { get; }
    }
}
