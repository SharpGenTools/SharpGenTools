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
                    Console.WriteLine("{0} file_path is expecting one file argument",
                                      typeof(Program).GetTypeInfo().Assembly.GetName().Name);
                    return 1;
                }

                string file = args[0];
                var program = new InteropApp();
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