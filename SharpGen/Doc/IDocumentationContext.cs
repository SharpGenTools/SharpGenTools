using SharpGen.Logging;

namespace SharpGen.Doc
{
    public interface IDocumentationContext
    {
        IDocItem CreateItem();

        IDocSubItem CreateSubItem();

        Logger Logger { get; }
    }
}