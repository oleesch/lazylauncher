using System;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;

using lazylauncher.Model;
using Microsoft.Win32;

namespace lazylauncher
{
    enum ExitCode
    {
        UnhandledException = 100000,
        ConfigMissing,
        OperationError
    }
    class Program
    {
        private static string _regKey = Path.Combine("HKEY_CURRENT_USER", "SOFTWARE", "lazylauncher");

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Process process = Process.GetCurrentProcess();
            string processPath = process.MainModule.FileName;
            string processDirPath = Path.GetDirectoryName(processPath);
            string configPath = Path.Combine(processDirPath, "llconfig.json");

            if (!File.Exists(configPath))
            {
                ExitWithError($"Could not find file at path: {configPath}", ExitCode.ConfigMissing);
            }
            string rawConfig = File.ReadAllText(configPath);
            LauncherConfig config = JsonConvert.DeserializeObject<LauncherConfig>(rawConfig);

            foreach (CopyOperation copyOp in config.CopyOperations)
            {
                if (HasBeenCompleted(copyOp.ID)) continue;

                CopyFolder(copyOp.OriginPath, copyOp.DestinationPath);

                MarkAsCompleted(copyOp.ID);
            }

            ProcessStartInfo startInfo = new ProcessStartInfo(config.ExecutablePath);
            startInfo.WorkingDirectory = config.WorkingDirPath;
            startInfo.Arguments = string.Join(" ", args);

            Process p = Process.Start(startInfo);
            p.WaitForExit();
            Environment.Exit(p.ExitCode);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ExitWithError(e.ExceptionObject.ToString(), ExitCode.UnhandledException);
        }

        static void CopyFolder(in string originPath, in string destPath)
        {
            if (!Directory.Exists(originPath))
            {
                ExitWithError($"Can't find origin path: {originPath}", ExitCode.OperationError);
            }

            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
                Console.WriteLine($"Created directory: {destPath}");
            }

            foreach (string dirPath in Directory.EnumerateDirectories(originPath))
            {
                CopyFolder(dirPath, Path.Combine(destPath, Path.GetFileName(dirPath)));
            }

            foreach (string filePath in Directory.EnumerateFiles(originPath))
            {
                try
                {
                    File.Copy(filePath, Path.Combine(destPath, Path.GetFileName(filePath)), true);
                    Console.WriteLine($"Copied file: {filePath}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Unable to copy file at path: {filePath}");
                    Console.Error.WriteLine(ex.ToString());
                }
            }
        }

        static bool HasBeenCompleted(in string id)
        {
            return Registry.GetValue(_regKey, id, null) != null;
        }

        static void MarkAsCompleted(in string id)
        {
            Registry.SetValue(_regKey, id, DateTime.UtcNow.ToString("o"));
        }

        static void ExitWithError(in string message, in ExitCode exitCode)
        {
            Console.Error.WriteLine(message);
            Environment.Exit((int)exitCode);
        }
    }
}
