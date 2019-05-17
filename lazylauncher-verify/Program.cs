using System;
using System.IO;

namespace lazylauncher_verify
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (string rawPath in args)
            {
                string path = Path.GetFullPath(Environment.ExpandEnvironmentVariables(rawPath));
                if (File.Exists(path))
                {
                    Console.WriteLine($"Found file: {path}");
                }
                else if (Directory.Exists(path))
                {
                    Console.WriteLine($"Found directory: {path}");
                }
                else
                {
                    Console.Error.WriteLine($"Unable to find: {path}");
                }
            }
            Console.Write("Any key to exit...");

            _ = Console.ReadKey(true);
        }
    }
}
