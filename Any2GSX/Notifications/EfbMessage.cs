namespace Any2GSX.Notifications
{
    public class EfbMessage
    {
        public virtual string WasmWorkaround { get; set; } = "derp";
        public virtual string CouatlVarsValid { get; set; }
        public virtual string ConnectionState { get; set; }
        public virtual string ProfileName { get; set; }
        public virtual string PhaseStatus { get; set; }
        public virtual string DepartureServices { get; set; }
        public virtual string SmartCall { get; set; }
        public virtual string ProgressLabel { get; set; }
        public virtual string ProgressInfo { get; set; }
        public virtual string MenuTitle { get; set; }
        public virtual string[] MenuLines { get; set; }
    }
}
