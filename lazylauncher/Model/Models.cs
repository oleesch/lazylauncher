using Newtonsoft.Json;
using System.ComponentModel;

namespace lazylauncher.Model
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

    enum EnvironmentVariableAction
    {
        Append,
        Replace
    }

    class EnvironmentVariable
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public EnvironmentVariableAction Action { get; set; }
    }

    class LauncherConfig
    {
        public string ID { get; set; }
        public string ExecutablePath { get; set; }
        public string WorkingDirPath { get; set; }
        public string Arguments { get; set; }

        [DefaultValue(false)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool UseShellExecute { get; set; }
        public CopyOperation[] CopyOperations { get; set; }
        public RegistryOperation[] RegistryOperations { get; set; }
        public EnvironmentVariable[] EnvironmentVariables { get; set; }
    }
}
