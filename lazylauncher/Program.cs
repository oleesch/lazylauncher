﻿using System;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;

using lazylauncher.Model;
using Microsoft.Win32;
using System.Threading;
using System.Text.RegularExpressions;

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
        private static readonly string _regKey = Path.Combine("HKEY_CURRENT_USER", "SOFTWARE", "lazylauncher");
        private static readonly string _logPath = Path.Combine(Environment.GetEnvironmentVariable("temp"), "lazylauncher.log");

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Overwriting the logfile on launch seems sensible as writes to %temp% aren't affected by copy on write
            if (File.Exists(_logPath))
            {
                File.Delete(_logPath);
            }

            Process process = Process.GetCurrentProcess();
            string processPath = process.MainModule.FileName;
            string processDirPath = Path.GetDirectoryName(processPath);
            Directory.SetCurrentDirectory(processDirPath);

            string configPath = Path.Combine(processDirPath, "llconfig.json");

            if (!File.Exists(configPath))
            {
                ExitWithError($"Could not find file at path: {configPath}", ExitCode.ConfigMissing);
            }
            WriteLog($"Reading config from path [{configPath}]");
            string rawConfig = File.ReadAllText(configPath);
            LauncherConfig config = JsonConvert.DeserializeObject<LauncherConfig>(rawConfig);

            if (!HasBeenCompleted(config.ID))
            {
                ProcessCopyOperations(config.CopyOperations);
                ProcessRegistryOperations(config.RegistryOperations);
                MarkAsCompleted(config.ID);
            }

            if (File.Exists(config.ExecutablePath))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(config.ExecutablePath);
                startInfo.WorkingDirectory = string.IsNullOrEmpty(config.WorkingDirPath) ? processDirPath : config.WorkingDirPath;
                string arguments = config.Arguments;
                if (args.Length > 0)
                {
                    foreach (var arg in args)
                    {
                        arguments += " \"" + arg + "\"";
                    }
                }
                startInfo.Arguments = arguments;
                startInfo.WindowStyle = ProcessWindowStyle.Normal;

                var useShellExeute = config.UseShellExecute;

                foreach (var variable in config.EnvironmentVariables)
                {
                    // When modifying environment variables, using shell execute is illegal!
                    useShellExeute = false;

                    var value = variable.Value
                        .Replace("{llWorkingDirPath}", startInfo.WorkingDirectory)
                        .Replace("{llRootPath}", processDirPath);

                    if ((startInfo.EnvironmentVariables.ContainsKey(variable.Name)) 
                        && (variable.Action == EnvironmentVariableAction.Append))
                    {
                        WriteLog($"Appending environment variable [{variable.Name}] with value [{value}]");
                        startInfo.EnvironmentVariables[variable.Name] += value;
                    }
                    else
                    {
                        WriteLog($"Setting environment variable [{variable.Name}] to value [{value}]");
                        startInfo.EnvironmentVariables[variable.Name] = value;
                    }
                }

                if (!(useShellExeute))
                {
                    startInfo.UseShellExecute = false;
                    WriteLog($"Start process [{startInfo.FileName}] with arguments [{startInfo.Arguments}] and working dir [{startInfo.WorkingDirectory}]");
                    Process p = Process.Start(startInfo);
                    p.WaitForExit();
                    WriteLog($"Process exited with exit code: {p.ExitCode}");
                    Environment.Exit(p.ExitCode);
                } else
                {
                    startInfo.UseShellExecute = true;
                    WriteLog($"Start process [{startInfo.FileName}] with arguments [{startInfo.Arguments}] and working dir [{startInfo.WorkingDirectory}] using shell execute");
                    Process.Start(startInfo);
                    Environment.Exit(0);
                }
            }
            else
            {
                ExitWithError($"Executable path does not exist: {config.ExecutablePath}", ExitCode.ExecutableMissing);
            }
        }

        private static void WriteLog(string message, in bool error = false)
        {
            if (error)
            {
                message = $"ERROR: {message}";
                Console.Error.WriteLine(message);
            }
            else
            {
                Console.WriteLine(message);
            }
            File.AppendAllLines(_logPath, new string[] { string.Format("{0} > {1}", DateTime.Now.ToString("o"), message) });
        }

        private static void ProcessCopyOperations(in CopyOperation[] copyOperations)
        {
            foreach (CopyOperation copyOp in copyOperations)
            {
                string originPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(copyOp.OriginPath));
                string destinationPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(copyOp.DestinationPath));
                WriteLog($"Copy from [{originPath}] to [{destinationPath}]");
                CopyFolder(originPath, destinationPath);
            }
        }

        private static void ProcessRegistryOperations(in RegistryOperation[] registryOperations)
        {
            foreach (RegistryOperation registryOp in registryOperations)
            {
                RegistryValueKind valueKind = new RegistryValueKind();
                if (Enum.TryParse(registryOp.ValueKind, out valueKind))
                {
                    WriteLog($"Set value [{registryOp.ValueName}] at key [{registryOp.KeyName}] to [{registryOp.Value}] as [{valueKind}]");
                    Registry.SetValue(registryOp.KeyName, registryOp.ValueName, registryOp.Value, valueKind);
                }
                else
                {
                    ExitWithError($"Error parsing value kind: {registryOp.ValueKind}", ExitCode.OperationError);
                }
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
                WriteLog($"Created directory: {destPath}");
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
                    WriteLog($"Copied file: {filePath}");
                }
                catch (Exception ex)
                {
                    WriteLog($"Unable to copy file at path: {filePath}", true);
                    WriteLog(ex.ToString(), true);
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
            WriteLog(message, true);
            Thread.Sleep(5000);
            Environment.Exit((int)exitCode);
        }
    }
}
