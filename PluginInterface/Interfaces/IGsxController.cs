using System;
using System.Threading.Tasks;

namespace Any2GSX.PluginInterface.Interfaces
{
    public interface IGsxController
    {
        public IGsxMenu IMenu { get; }
        public IGsxAutomationController IAutomationController { get; }
        public bool IsGateConnected { get; }
        public bool HasGateJetway { get; }
        public bool HasGateStair { get; }
        public bool HasUndergroundRefuel { get; }
        public bool ServicesValid { get; }
        public bool IsDeiceAvail { get; }

        public bool CouatlVarsValid { get; }
        public bool CouatlVarsReceived { get; }
        public bool IsMsfs2024 { get; }
        public bool IsProcessRunning { get; }
        public bool IsActive { get; }
        public bool IsGsxRunning { get; }
        public bool IsOnGround { get; }
        public bool IsAirStart { get; }
        public bool CanAutomationRun { get; }
        public bool IsPaused { get; }
        public bool IsWalkaround { get; }
        public bool SkippedWalkAround { get; }
        public event Func<Task> WalkaroundPreAction;
        public event Func<Task> WalkaroundWasSkipped;
        public Task ToggleWalkaround();
        public Task SetPaxBoard(int pax);
        public Task SetPaxDeboard(int pax);
        public IGsxService GetService(GsxServiceType type);
        public int JetwayState { get; }
        public int JetwayOperation { get; }
        public int StairsState { get; }
        public int StairsOperation { get; }
        public bool TryGetService(GsxServiceType type, out IGsxService gsxService);
    }
}
