using Microsoft.Win32;
using System;
using System.IO;

namespace lazylauncher_verify
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                foreach (string arg in args)
                {
                    Console.WriteLine(arg);
                }
            }

            string[] pathsToVerify = new string[]
            {
                "%APPDATA%\\lazylauncher",
                "%APPDATA%\\lazylauncher\\someConfigDir",
                "%APPDATA%\\lazylauncher\\someConfigFile.txt"
            };
            foreach (string rawPath in pathsToVerify)
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

            string[] environmentVariablesToVerify = new string[]
            {
                "PATH",
                "LL_EXAMPLE"
            };

            foreach (var variable in environmentVariablesToVerify)
            {
                var value = Environment.GetEnvironmentVariable(variable);
                if (!string.IsNullOrEmpty(value))
                {
                    Console.WriteLine($"Found variable [{variable}] with content: {value}");
                }
                else
                {
                    Console.Error.WriteLine($"Unable to find environment variable: {variable}");
                }
            }

            Console.Write("Any key to exit...");
            _ = Console.ReadKey(true);
        }
    }
}
