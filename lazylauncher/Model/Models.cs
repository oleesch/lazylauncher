namespace lazylauncher.Model
{
    interface IOperation
    {
        string ID { get; }
    }

    class CopyOperation: IOperation
    {
        public string ID { get; set; }
        public string OriginPath { get; set; }
        public string DestinationPath { get; set; }
    }

    class LauncherConfig
    {
        public string ID { get; set; }
        public string ExecutablePath { get; set; }
        public string WorkingDirPath { get; set; }
        public CopyOperation[] CopyOperations { get; set; }
    }
}
