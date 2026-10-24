using Any2GSX.Aircraft;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation
{
    public class AircraftState
    {
        public virtual AircraftController AircraftController => AppService.Instance.AircraftController;
        public virtual AircraftBase Aircraft => AircraftController.Aircraft;
        public virtual bool IsConnected => AircraftController.IsAircraftConnected;
        public virtual bool HasSmartButtonRequest { get; protected set; } = false;
        public virtual bool ReadyDepartureServices { get; protected set; } = false;
        public virtual bool IsBoardingCompleted { get; protected set; } = false;
        public virtual bool IsCargo { get; protected set; } = false;
        public virtual bool HasFuelSync { get; protected set; } = false;
        public virtual bool HasCustomStairs { get; protected set; } = false;
        public virtual bool CustomStairsFront { get; protected set; } = false;
        public virtual bool CustomStairsRear { get; protected set; } = false;
        public virtual bool HasAirStairs { get; protected set; } = false;
        public virtual bool AirstairsState => AirstairForward || AirstairAft;
        public virtual bool AirstairForward { get; protected set; } = false;
        public virtual bool AirstairAft { get; protected set; } = false;
        public virtual bool HasChocks { get; protected set; } = false;
        public virtual bool EquipmentChocks { get; protected set; } = false;
        public virtual bool BrakeSet { get; protected set; } = false;
        public virtual bool HasCones { get; protected set; } = false;
        public virtual bool EquipmentCones { get; protected set; } = false;
        public virtual bool HasGpuInternal { get; protected set; } = false;
        public virtual bool GpuRequireChocks { get; protected set; } = false;
        public virtual GsxGpuUsage UseGpuGsx { get; protected set; } = GsxGpuUsage.Never;
        public virtual bool EquipmentGpu { get; protected set; } = false;
        public virtual bool AvionicsPowered { get; protected set; } = false;
        public virtual bool PowerConnected { get; protected set; } = false;
        public virtual bool HasPca { get; protected set; } = false;
        public virtual bool PcaRequirePower { get; protected set; } = false;
        public virtual bool EquipmentPca { get; protected set; } = false;
        public virtual bool ApuRunning { get; protected set; } = false;
        public virtual bool ApuBleed { get; protected set; } = false;
        public virtual bool LightBeacon { get; protected set; } = false;
        public virtual bool LightNav { get; protected set; } = false;
        public virtual double Speed { get; protected set; } = 0;
        public virtual double FuelOnBoard { get; protected set; } = 0;
        public virtual double ZeroFuel { get; protected set; } = 0;
        public virtual double WeightTotal { get; protected set; } = 0;
        public virtual bool EnginesRunning { get; protected set; } = false;
        public virtual bool LastBrake { get; set; } = false;
        public virtual bool BrakeChanged => LastBrake != BrakeSet;

        public virtual void Reset()
        {
            ReadyDepartureServices = false;
            HasSmartButtonRequest = false;
            IsBoardingCompleted = false;
            IsCargo = false;
            HasFuelSync = false;
            HasCustomStairs = false;
            CustomStairsFront = false;
            CustomStairsRear = false;
            HasAirStairs = false;
            AirstairForward = false;
            AirstairAft = false;
            LightBeacon = false;
            LightNav = false;
            Speed = 0;
            FuelOnBoard = 0;
            ZeroFuel = 0;
            WeightTotal = 0;
            EnginesRunning = false;
            LastBrake = false;
            HasCones = false;
            EquipmentCones = false;
            BrakeSet = false;
            HasChocks = false;
            EquipmentChocks = false;
            HasGpuInternal = false;
            GpuRequireChocks = false;
            UseGpuGsx = GsxGpuUsage.Never;
            EquipmentGpu = false;
            AvionicsPowered = false;
            PowerConnected = false;
            HasPca = false;
            PcaRequirePower = false;
            EquipmentPca = false;
            ApuRunning = false;
            ApuBleed = false;
        }

        public virtual async Task Refresh()
        {
            if (IsConnected)
            {
                if (Speed < AppService.Instance.Config.SpeedTresholdTaxiOut)
                    LastBrake = BrakeSet;

                ReadyDepartureServices = await Aircraft.GetReadyDepartureServices();
                HasSmartButtonRequest = await Aircraft.GetSmartButtonRequest() || AppService.Instance.GsxController.SubDefaultSmartButton?.GetNumber() != 0;
                IsBoardingCompleted = await Aircraft.GetIsBoardingCompleted();
                IsCargo = await Aircraft.GetIsCargo();
                HasCustomStairs = await Aircraft.HasCustomStairs();
                CustomStairsFront = await Aircraft.GetCustomStairsFront();
                CustomStairsRear = await Aircraft.GetCustomStairsRear();
                HasFuelSync = await Aircraft.GetHasFuelSync();
                HasAirStairs = await Aircraft.GetHasAirStairs();
                AirstairForward = await Aircraft.GetAirStairForward();
                AirstairAft = await Aircraft.GetAirStairAft();
                LightBeacon = await Aircraft.GetLightBeacon();
                LightNav = await Aircraft.GetLightNav();
                Speed = await Aircraft.GetSpeed();
                FuelOnBoard = await Aircraft.GetFuelOnBoardKg();
                ZeroFuel = await Aircraft.GetWeightZeroFuelKg();
                WeightTotal = await Aircraft.GetWeightTotalKg();
                EnginesRunning = await Aircraft.GetEngineRunning();
                HasCones = await Aircraft.GetHasCones();
                EquipmentCones = await Aircraft.GetEquipmentCones();
                BrakeSet = await Aircraft.GetBrakeSet();
                HasChocks = await Aircraft.GetHasChocks();
                EquipmentChocks = await Aircraft.GetEquipmentChocks();
                HasGpuInternal = await Aircraft.GetHasGpuInternal();
                GpuRequireChocks = await Aircraft.GetGpuRequireChocks();
                UseGpuGsx = await Aircraft.GetUseGpuGsx();
                EquipmentGpu = await Aircraft.GetExternalPowerAvailable();
                AvionicsPowered = await Aircraft.GetAvionicPowered();
                PowerConnected = await Aircraft.GetExternalPowerConnected();
                HasPca = await Aircraft.GetHasPca();
                PcaRequirePower = await Aircraft.GetPcaRequirePower();
                EquipmentPca = await Aircraft.GetEquipmentPca();
                ApuRunning = await Aircraft.GetApuRunning();
                ApuBleed = await Aircraft.GetApuBleedOn();
            }
        }
    }
}
