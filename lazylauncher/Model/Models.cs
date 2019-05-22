﻿namespace lazylauncher.Model
{
    class CopyOperation
    {
        public string OriginPath { get; set; }
        public string DestinationPath { get; set; }
    }

    class RegistryOperation
    {
        public string KeyName { get; set; }
        public string ValueName { get; set; }
        public string Value { get; set; }
        public string ValueKind { get; set; }
    }

    class LauncherConfig
    {
        public string ID { get; set; }
        public string ExecutablePath { get; set; }
        public string WorkingDirPath { get; set; }
        public string Arguments { get; set; }
        public CopyOperation[] CopyOperations { get; set; }
        public RegistryOperation[] RegistryOperations { get; set; }
    }
}
