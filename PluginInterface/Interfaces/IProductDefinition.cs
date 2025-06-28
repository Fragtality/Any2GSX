namespace Any2GSX.PluginInterface.Interfaces
{
    public interface IProductDefinition
    {
        public string ProductName { get; }
        public string ProductExePath { get; }
        public string PluginFolder { get; }
        public bool RequireSimRunning { get; }
        public bool WaitForSim { get; }
        public bool SingleInstance { get; }
        public bool MainWindowShowOnStartup { get; }
    }
}
