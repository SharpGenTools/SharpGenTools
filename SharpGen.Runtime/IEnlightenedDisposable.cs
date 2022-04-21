namespace SharpGen.Runtime;

public interface IEnlightenedDisposable
{
    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    void CheckAndDispose(bool disposing);
}