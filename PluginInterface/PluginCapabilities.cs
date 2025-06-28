namespace Any2GSX.PluginInterface
{
    public enum SynchType
    {
        ManualNone = 1,
        Aircraft = 2,
        GSX = 4,
        Plugin = 8,
    }

    public enum GroundEquipment
    {
        ManualNone = 1,
        GPU = 2,
        Chocks = 4,
        Cones = 8,
        Covers = 16,
        PCA = 32,
    }

    public class PluginCapabilities
    {
        public virtual bool HasFobSaveRestore { get; set; } = false;
        public virtual SynchType FuelSync { get; set; } = SynchType.ManualNone;
        public virtual bool CanSetPayload { get; set; } = false;
        public virtual SynchType PayloadSync { get; set; } = SynchType.ManualNone;
        public virtual SynchType DoorHandling { get; set; } = SynchType.ManualNone;
        public virtual GroundEquipment GroundEquipmentHandling { get; set; } = GroundEquipment.ManualNone;
        public virtual bool HasSmartButton { get; set; } = false;
        public virtual bool VolumeControl { get; set; } = false;
    }
}
