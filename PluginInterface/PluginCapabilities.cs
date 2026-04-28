using Any2GSX.PluginInterface.Interfaces;
using System.Text.Json.Serialization;

namespace Any2GSX.PluginInterface
{
    public enum SyncType
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

    public enum DoorTypeSynced
    {
        None = 1,
        PaxJetway = 2,
        PaxStairs = 4,
        Service = 8,
        Cargo = 16,
        Panels = 32,
    }

    public class PluginCapabilities
    {
        public virtual bool HasFobSaveRestore { get; set; } = false;
        public virtual SyncType FuelSync { get; set; } = SyncType.ManualNone;
        [JsonIgnore]
        public virtual bool HasFuelSync => FuelSync.HasFlag(SyncType.Plugin);
        public virtual bool CanSetPayload { get; set; } = false;
        public virtual SyncType PayloadSync { get; set; } = SyncType.ManualNone;
        [JsonIgnore]
        public virtual bool HasPayloadSync => PayloadSync.HasFlag(SyncType.Plugin);
        public virtual SyncType DoorHandling { get; set; } = SyncType.ManualNone;
        public virtual DoorTypeSynced DoorsSynced { get; set; } = DoorTypeSynced.None;
        public virtual bool DoorsCloseAll { get; set; } = false;
        [JsonIgnore]
        public virtual bool HasDoorSync => DoorHandling.HasFlag(SyncType.Plugin);
        [JsonIgnore]
        public virtual bool HasDoorSyncPax => DoorsSynced.HasFlag(DoorTypeSynced.PaxJetway) || DoorsSynced.HasFlag(DoorTypeSynced.PaxStairs);
        [JsonIgnore]
        public virtual bool HasDoorSyncPaxStair => DoorsSynced.HasFlag(DoorTypeSynced.PaxStairs);
        [JsonIgnore]
        public virtual bool HasDoorSyncService => DoorsSynced.HasFlag(DoorTypeSynced.Service);
        [JsonIgnore]
        public virtual bool HasDoorSyncCargo => DoorsSynced.HasFlag(DoorTypeSynced.Cargo);
        [JsonIgnore]
        public virtual bool HasPanelSync => DoorsSynced.HasFlag(DoorTypeSynced.Panels);
        public virtual GroundEquipment GroundEquipmentHandling { get; set; } = GroundEquipment.ManualNone;
        [JsonIgnore]
        public virtual bool HasGroundEquip => HasEquipChocks || HasEquipCones || HasEquipGpu || HasEquipPca;
        [JsonIgnore]
        public virtual bool HasEquipPca => GroundEquipmentHandling.HasFlag(GroundEquipment.PCA);
        [JsonIgnore]
        public virtual bool HasEquipGpu => GroundEquipmentHandling.HasFlag(GroundEquipment.GPU);
        [JsonIgnore]
        public virtual bool HasEquipChocks => GroundEquipmentHandling.HasFlag(GroundEquipment.Chocks);
        [JsonIgnore]
        public virtual bool HasEquipCones => GroundEquipmentHandling.HasFlag(GroundEquipment.Cones);
        public virtual bool HasSmartButton { get; set; } = false;
        public virtual bool VolumeControl { get; set; } = false;
        public virtual CockpitNotification CockpitNotifications { get; set; } = CockpitNotification.None;
    }
}
