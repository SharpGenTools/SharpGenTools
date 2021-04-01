using System.IO;
using System.Linq;
using SharpGen.Platform.Clang.CSharp;

namespace SharpGen.Platform.Clang
{
    public partial class PInvokeGenerator
    {
        public void Close()
        {
            foreach (var foundUuid in _uuidsToGenerate)
            {
                var iidName = foundUuid.Key;

                if (_generatedUuids.Contains(iidName))
                    continue;

                var iidValue = foundUuid.Value.ToString("X").ToUpperInvariant().Replace("{", "").Replace("}", "")
                                        .Replace('X', 'x').Replace(",", ", ");

                StartUsingOutputBuilder(_config.MethodClassName);

                _outputBuilder.WriteIid(iidName, iidValue);

                StopUsingOutputBuilder();
            }

            foreach (CSharpOutputBuilder outputBuilder in _outputBuilderFactory.OutputBuilders)
            {
                var isMethodClass = _config.MethodClassName.Equals(outputBuilder.Name);

                var stream = _host.GetOutputStream(
                    outputBuilder.IsTestOutput, outputBuilder.Name, outputBuilder.Extension
                );

                using var sw = new StreamWriter(stream, defaultStreamWriterEncoding, DefaultStreamWriterBufferSize,
                                                true)
                {
                    NewLine = "\n"
                };

                if (_config.HeaderText != string.Empty)
                {
                    sw.WriteLine(_config.HeaderText);
                }

                var usingDirectives =
                    outputBuilder.UsingDirectives.Concat(outputBuilder.StaticUsingDirectives);

                if (usingDirectives.Any())
                {
                    foreach (var usingDirective in usingDirectives)
                    {
                        sw.Write("using ");
                        sw.Write(usingDirective);
                        sw.WriteLine(';');
                    }

                    sw.WriteLine();
                }

                var indentationString = outputBuilder.IndentationString;

                sw.Write("namespace ");
                sw.Write(Config.Namespace);

                if (outputBuilder.IsTestOutput)
                {
                    sw.Write(".UnitTests");
                }

                sw.WriteLine();
                sw.WriteLine('{');

                if (isMethodClass)
                {
                    sw.Write(indentationString);
                    sw.Write("public static ");

                    if (_isMethodClassUnsafe)
                    {
                        sw.Write("unsafe ");
                    }

                    sw.Write("partial class ");
                    sw.WriteLine(Config.MethodClassName);
                    sw.Write(indentationString);
                    sw.WriteLine('{');

                    indentationString += outputBuilder.IndentationString;
                }

                foreach (var line in outputBuilder.Contents)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        sw.WriteLine();
                    }
                    else
                    {
                        sw.Write(indentationString);
                        sw.WriteLine(line);
                    }
                }

                if (isMethodClass)
                {
                    sw.Write(outputBuilder.IndentationString);
                    sw.WriteLine('}');
                }

                sw.WriteLine('}');
            }

            _context.Clear();
            _diagnostics.Clear();
            _fileContentsBuilder.Clear();
            _generatedUuids.Clear();
            _outputBuilderFactory.Clear();
            _uuidsToGenerate.Clear();
            _visitedFiles.Clear();
        }
    }
}