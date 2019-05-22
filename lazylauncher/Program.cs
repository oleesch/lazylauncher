using System;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;

using lazylauncher.Model;
using Microsoft.Win32;
using System.Threading;

namespace lazylauncher
{
    enum ExitCode
    {
        UnhandledException = 100000,
        ConfigMissing,
        OperationError,
        ExecutableMissing
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

            ProcessCopyOperations(config.CopyOperations);
            ProcessRegistryOperations(config.RegistryOperations);

            if (File.Exists(config.ExecutablePath))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(config.ExecutablePath);
                startInfo.WorkingDirectory = config.WorkingDirPath;
                string arguments = config.Arguments;
                if (args.Length > 0)
                {
                    arguments = arguments + " " + string.Join(" ", args);
                }
                startInfo.Arguments = arguments;
                startInfo.UseShellExecute = false;
                startInfo.WindowStyle = ProcessWindowStyle.Normal;

                Process p = Process.Start(startInfo);
                p.WaitForExit();
                Environment.Exit(p.ExitCode);
            }
            else
            {
                ExitWithError($"Executable path does not exist: {config.ExecutablePath}", ExitCode.ExecutableMissing);
            }
        }

        private static void ProcessCopyOperations(in CopyOperation[] copyOperations)
        {
            foreach (CopyOperation copyOp in copyOperations)
            {
                if (HasBeenCompleted(copyOp.ID)) continue;

                string originPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(copyOp.OriginPath));
                string destinationPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(copyOp.DestinationPath));
                CopyFolder(originPath, destinationPath);

                MarkAsCompleted(copyOp.ID);
            }
        }

        private static void ProcessRegistryOperations(in RegistryOperation[] registryOperations)
        {
            foreach (RegistryOperation registryOp in registryOperations)
            {
                if (HasBeenCompleted(registryOp.ID)) continue;

                RegistryValueKind valueKind = new RegistryValueKind();
                if (Enum.TryParse(registryOp.ValueKind, out valueKind))
                {
                    Registry.SetValue(registryOp.KeyName, registryOp.ValueName, registryOp.Value, valueKind);
                }
                else
                {
                    ExitWithError($"Error parsing value kind: {registryOp.ValueKind}", ExitCode.OperationError);
                }

                MarkAsCompleted(registryOp.ID);
            }
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
            Thread.Sleep(5000);
            Environment.Exit((int)exitCode);
        }
    }
}
