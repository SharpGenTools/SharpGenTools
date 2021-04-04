// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SharpGen.Logging;
using SharpGen.Parser;

namespace SharpGen.Platform
{
    /// <summary>
    /// CastXML front end for command line.
    /// see https://github.com/CastXML/CastXML
    /// </summary>
    public sealed class CastXmlRunner : ICastXmlRunner
    {
        private static readonly Regex MatchError = new Regex("error:", RegexOptions.Compiled);

        // path/to/header.h:68:1: error:
        private static readonly Regex MatchFileErrorRegex = new Regex(@"^(.*):(\d+):(\d+):\s+error:(.*)", RegexOptions.Compiled);

        private IReadOnlyList<string> AdditionalArguments { get; }

        public string OutputPath { get; set; }

        /// <summary>
        /// Gets or sets the executable path of castxml.
        /// </summary>
        /// <value>The executable path.</value>
        private string ExecutablePath { get; }

        private Logger Logger { get; }

        private readonly IncludeDirectoryResolver directoryResolver;

        public CastXmlRunner(Logger logger, IncludeDirectoryResolver directoryResolver,
                             string executablePath, IReadOnlyList<string> additionalArguments)
        {
            this.directoryResolver = directoryResolver ?? throw new ArgumentNullException(nameof(directoryResolver));
            AdditionalArguments = additionalArguments ?? throw new ArgumentNullException(nameof(additionalArguments));
            ExecutablePath = executablePath ?? throw new ArgumentNullException(nameof(executablePath));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Preprocesses the specified header file.
        /// </summary>
        /// <param name="headerFile">The header file.</param>
        /// <param name="handler">The handler.</param>
        public void Preprocess(string headerFile, CastXmlPreprocessedLineReceivedEventHandler handler)
        {
            void OutputDataCallback(object sender, DataReceivedEventArgs data) => handler(data.Data);

            Logger.RunInContext(nameof(Preprocess), () =>
            {
                if (!File.Exists(ExecutablePath))
                    Logger.Fatal("castxml not found from path: [{0}]", ExecutablePath);

                if (!File.Exists(headerFile))
                    Logger.Fatal("C++ Header file [{0}] not found", headerFile);

                RunCastXml(headerFile, OutputDataCallback, "-E -dD");
            });
        }

        /// <summary>
        /// Processes the specified header headerFile.
        /// </summary>
        /// <param name="headerFile">The header headerFile.</param>
        /// <returns></returns>
        public StreamReader Process(string headerFile)
        {
            StreamReader result = null;

            Logger.RunInContext(nameof(Process), () =>
            {
                if (!File.Exists(ExecutablePath)) Logger.Fatal("castxml not found from path: [{0}]", ExecutablePath);

                if (!File.Exists(headerFile)) Logger.Fatal("C++ Header file [{0}] not found", headerFile);

                var xmlFile = Path.ChangeExtension(headerFile, "xml");

                // Delete any previously generated xml file
                File.Delete(xmlFile);

                RunCastXml(headerFile, LogCastXmlOutput, $"-o {xmlFile}");

                if (!File.Exists(xmlFile) || Logger.HasErrors)
                {
                    Logger.Error(LoggingCodes.CastXmlFailed, "Unable to generate XML file with castxml [{0}]. Check previous errors.", xmlFile);
                }
                else
                {
                    result = File.OpenText(xmlFile);
                }
            });

            return result;
        }

        private void RunCastXml(string headerFile, DataReceivedEventHandler outputDataCallback, string additionalArguments)
        {
            var arguments = GetCastXmlArgs().Append(additionalArguments)
                                            .Concat(directoryResolver.IncludeArguments);
            var argumentsString = string.Join(" ", arguments);

            Logger.Message("CastXML {0}", argumentsString);
            using var currentProcess = new Process
            {
                StartInfo = new ProcessStartInfo(ExecutablePath)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = OutputPath,
                    Arguments = $"{argumentsString} \"{headerFile}\""
                }
            };
            currentProcess.ErrorDataReceived += ProcessErrorFromHeaderFile;
            currentProcess.OutputDataReceived += outputDataCallback;
            currentProcess.Start();
            currentProcess.BeginOutputReadLine();
            currentProcess.BeginErrorReadLine();

            currentProcess.WaitForExit();

            if (Logger.HasErrors)
            {
                Logger.Error(LoggingCodes.CastXmlFailed, "Failed to run CastXML. Check previous errors.");
            }
        }

        private IEnumerable<string> GetCastXmlArgs()
        {
            return AdditionalArguments.Append("--castxml-gccxml")
                                      .Append("-x c++")
                                      .Append("-Wmacro-redefined")
                                      .Append("-Wno-invalid-token-paste")
                                      .Append("-Wno-ignored-attributes");
        }

        /// <summary>
        /// Processes the error from header file.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Diagnostics.DataReceivedEventArgs"/> instance containing the event data.</param>
        private void ProcessErrorFromHeaderFile(object sender, DataReceivedEventArgs e)
        {
            var popContext = false;
            try
            {
                if (e.Data != null)
                {
                    var matchError = MatchFileErrorRegex.Match(e.Data);

                    var errorText = e.Data;

                    if (matchError.Success)
                    {
                        Logger.PushLocation(matchError.Groups[1].Value, int.Parse(matchError.Groups[2].Value), int.Parse(matchError.Groups[3].Value));
                        popContext = true;
                        errorText = matchError.Groups[4].Value;
                    }

                    if (MatchError.Match(e.Data).Success)
                        Logger.Error(LoggingCodes.CastXmlError, errorText);
                    else
                        Logger.Warning(LoggingCodes.CastXmlWarning, errorText);
                }
            }
            finally
            {
                if (popContext)
                    Logger.PopLocation();
            }
        }

        /// <summary>
        /// Processes the output from header file.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Diagnostics.DataReceivedEventArgs"/> instance containing the event data.</param>
        private void LogCastXmlOutput(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
                Logger.Message(e.Data);
        }
    }
}