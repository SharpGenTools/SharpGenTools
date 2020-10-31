using System.IO;

#nullable enable

namespace SharpGen.Parser
{
    public delegate void CastXmlPreprocessedLineReceivedEventHandler(string line);

    public interface ICastXmlRunner
    {
        /// <summary>
        /// Preprocesses the specified header file.
        /// </summary>
        /// <param name="headerFile">The header file.</param>
        /// <param name="handler">The handler.</param>
        void Preprocess(string headerFile, CastXmlPreprocessedLineReceivedEventHandler handler);

        /// <summary>
        /// Processes the specified header headerFile.
        /// </summary>
        /// <param name="headerFile">The header headerFile.</param>
        /// <returns></returns>
        StreamReader Process(string headerFile);
    }
}