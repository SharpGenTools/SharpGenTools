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
using System.Threading;
using System.Windows.Forms;
using SharpGen.Logging;
using Mono.Options;

namespace SharpGen.Interactive
{
    /// <summary>
    /// Main program for CodeGen
    /// </summary>
    internal static class Program
    {
        private static CodeGenApp _codeGenApp;
        private static ProgressForm _progressForm;

        /// <summary>
        /// Runs code generation asynchronously.
        /// </summary>
        public static void RunAsync()
        {
            try
            {
                Logger.Progress(0, "Starting code generation...");

                _codeGenApp.Run();
            }
            catch(Exception ex)
            {
                Logger.Fatal("Unexpected exception", ex);
            }
            finally
            {
                if(_progressForm != null)
                {
                    MethodInvoker methodInvoker = delegate() { _progressForm.Shutdown(); };
                    _progressForm.Invoke(methodInvoker);
                }
            }
        }

        /// <summary>
        /// Parses the command line arguments.
        /// </summary>
        /// <param name="args">The args.</param>
        public static void ParseArguments(string[] args, CodeGenApp app)
        {
            var showHelp = false;

            var options = new OptionSet()
                              {
                                  "Copyright (c) 2010-2014 SharpDX - Alexandre Mutel",
                                  "Usage: SharpGen [options] config_file.xml",
                                  "Code generator from C++ to C# for .Net languages",
                                  "",
                                  {"c|castxml=", "Specify the path to castxml.exe", opt => app.CastXmlExecutablePath = opt},
                                  {"d|doc", "Specify to generate the documentation [default: false]", opt => app.IsGeneratingDoc = true},
                                  {"p|docpath=", "Specify the path to the assembly doc provider [default: null]", opt => app.DocProviderAssemblyPath = opt},
                                  {"v|vctools=", "Specify the path to the Visual C++ Toolset", opt => app.VcToolsPath = opt },
                                  {"od|outputdir=", "Specify the base output directory for the generated code", opt => app.OutputDirectory = opt },
                                  {"a|apptype=", "Specify what app type to generate code for (i.e. DESKTOP_APP or STORE_APP)", opt => app.AppType = opt },
                                  {"D:", "Define a macro that is used in the config files", m => app.Macros.Add(m) },
                                  "",
                                  {"h|help", "Show this message and exit", opt => showHelp = opt != null},
                                  // default
                                  {"<>", opt => app.ConfigRootPath = opt },
                              };
            try
            {
                options.Parse(args);
            }
            catch (OptionException e)
            {
                UsageError(e.Message);
            }

            if (showHelp)
            {
                options.WriteOptionDescriptions(Console.Out);
                Environment.Exit(0);
            }

            if (app.ConfigRootPath == null)
                UsageError("Missing config.xml. A config.xml must be specified");
            if (app.AppType == null)
                UsageError("Missing apptype argument. an App type must be specified (for example: -apptype=DESKTOP_APP");
        }

        /// <summary>
        /// Print usages the error.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <param name="parameters">The parameters.</param>
        private static void UsageError(string error, params object[] parameters)
        {
            Console.Write("SharpGen: ");
            Console.WriteLine(error, parameters);
            Console.WriteLine("Use SharpGen --help' for more information.");
            Environment.Exit(1);
        }

        /// <summary>
        /// Main SharpGen
        /// </summary>
        /// <param name="args">Command line args.</param>
        [STAThread]
        public static void Main(string[] args)
        {
            Logger.LoggerOutput = new ConsoleLogger();
            _progressForm = null;
            try
            {
                _codeGenApp = new CodeGenApp();
                ParseArguments(args, _codeGenApp);

                if(_codeGenApp.Init())
                {
                    if(Environment.GetEnvironmentVariable("SharpDXBuildNoWindow") == null)
                    {
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                        _progressForm = new ProgressForm();
                        Logger.ProgressReport = _progressForm;
                        _progressForm.Show();

                        var runningThread = new Thread(RunAsync) {IsBackground = true};
                        runningThread.Start();

                        Application.Run(_progressForm);
                    }
                    else
                    {
                        RunAsync();
                    }

                }
                else
                {
                    Logger.Message("Latest code generation is up to date. No need to run code generation");
                }

            }
            catch(Exception ex)
            {
                Logger.Fatal("Unexpected exception", ex);
            }
            Environment.Exit(0);
        }
    }
}