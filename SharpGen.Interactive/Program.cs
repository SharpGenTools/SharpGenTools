using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Logging.Serilog;
using Mono.Options;
using SharpGen.Logging;

namespace SharpGen.Interactive
{
    class Program
    {
        static void Main(string[] args)
        {
            var appBuilder = BuildAvaloniaApp().SetupWithoutStarting();
            var model = new SharpGenModel();
            var logger = new Logger(new ConsoleLogger(), new SharpGenModel.ProgressReporter(model));
            var codeGenApp = new CodeGenApp(logger)
            {
                GlobalNamespace = new GlobalNamespaceProvider("SharpGen.Runtime")
            };
            ParseArguments(args, codeGenApp);
            var window = new ProgressView(new SharpGenModel());
            window.Show();

            Task.Run(() =>
            {
                if (codeGenApp.Init())
                {
                    try
                    {
                        logger.Progress(0, "Starting code generation...");

                        codeGenApp.Run();
                    }
                    catch (Exception ex)
                    {
                        logger.Fatal("Unexpected exception", ex);
                    }
                    finally
                    {
                        Application.Current.Exit();
                    }
                }
            });

            appBuilder.Instance.Run(window);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug();


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
        /// Parses the command line arguments.
        /// </summary>
        /// <param name="args">The args.</param>
        private static void ParseArguments(string[] args, CodeGenApp app)
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
                                  {"od|outputdir=", "Specify the base output directory for the generated code", opt => app.OutputDirectory = opt },
                                  {"D:", "Define a macro that is used in the config files", m => app.Macros.Add(m) },
                                  {"g|global=", "Specify the namespace with the infrastructure types such as ComObject and FunctionCallback", opt => app.GlobalNamespace = new GlobalNamespaceProvider(opt) },
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
        }
    }
}
