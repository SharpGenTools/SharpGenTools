using System;
using System.Reflection;

namespace SharpPatch.Cli
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                if (args.Length != 1)
                {
                    Console.WriteLine($"{typeof(Program).GetTypeInfo().Assembly.GetName().Name} file_path is expecting one file argument");
                    return 1;
                }

                var file = args[0];
                var program = new InteropApp
                {
                    Logger = new Logger()
                };
                program.PatchFile(file);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }
            return 0;
        }
    }
}