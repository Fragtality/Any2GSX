using CFIT.SimConnectLib.SimResources;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Any2GSX.PluginInterface.Interfaces
{
    public interface IGsxController
    {
        public CancellationToken RequestToken { get; }
        public string PathInstallation { get; }
        public bool IsMsfs2024 { get; }
        public IGsxMenu IMenu { get; }
        public IGsxAutomationController IAutomationController { get; }
        public AutomationState AutomationState { get; }

        public event Func<IGsxController, Task> OnCouatlStarted;
        public event Func<IGsxController, Task> OnCouatlStopped;

        public GsxServiceState JetwayState { get; }
        public GsxServiceState JetwayOperation { get; }
        public GsxServiceState StairsState { get; }
        public GsxServiceState StairsOperation { get; }
        public bool IsStairConnected { get; }

        public bool IsGateConnected { get; }
        public bool HasGateJetway { get; }
        public bool HasGateStair { get; }
        public bool HasUndergroundRefuel { get; }
        public bool ServicesValid { get; }
        public bool IsDeiceAvail { get; }
        public bool IsRefuelActive { get; }
        public bool IsFuelHoseConnected { get; }

        public bool CouatlVarsValid { get; }
        public bool CouatlVarsReceived { get; }
        public bool IsProcessRunning { get; }
        public bool IsActive { get; }
        public bool IsGsxRunning { get; }
        public bool IsOnGround { get; }
        public bool IsAirStart { get; }
        public bool CanAutomationRun { get; }
        public bool IsPaused { get; }
        public bool IsWalkaround { get; }
        public bool SkippedWalkAround { get; }
        public bool WalkAroundSkipActive { get; }
        public bool WalkaroundPreActionNotified { get; }
        public bool WalkaroundNotified { get; }

        public event Func<Task> WalkaroundPreAction;
        public event Func<Task> WalkaroundWasSkipped;
        public event Func<Task> RepositionSignal;

        public ISimResourceSubscription SubDoorToggleExit1 { get; }
        public ISimResourceSubscription SubDoorToggleExit2 { get; }
        public ISimResourceSubscription SubDoorToggleExit3 { get; }
        public ISimResourceSubscription SubDoorToggleExit4 { get; }
        public ISimResourceSubscription SubDoorToggleService1 { get; }
        public ISimResourceSubscription SubDoorToggleService2 { get; }
        public ISimResourceSubscription SubDoorToggleCargo1 { get; }
        public ISimResourceSubscription SubDoorToggleCargo2 { get; }
        public ISimResourceSubscription SubDoorToggleCargo3 { get; }
        public ISimResourceSubscription SubLoaderAttachCargo1 { get; }
        public ISimResourceSubscription SubLoaderAttachCargo2 { get; }
        public ISimResourceSubscription SubLoaderAttachCargo3 { get; }

        public IGsxService GetService(GsxServiceType type);
        public bool TryGetService(GsxServiceType type, out IGsxService gsxService);
        public DateTime GetTime();

        public Task ToggleWalkaround();
        public Task ReloadSimbrief();
        public Task SetPaxBoard(int pax);
        public Task SetPaxDeboard(int pax);
        public Task CancelRefuel();

        public static bool IsCargoDoor(GsxDoor door)
        {
            return door == GsxDoor.CargoDoor1 || door == GsxDoor.CargoDoor2 || door == GsxDoor.CargoDoor3Main;
        }

        public static bool IsPaxDoor(GsxDoor door)
        {
            return door == GsxDoor.PaxDoor1 || door == GsxDoor.PaxDoor2 || door == GsxDoor.PaxDoor3 || door == GsxDoor.PaxDoor4;
        }

        public static bool IsServiceDoor(GsxDoor door)
        {
            return door == GsxDoor.ServiceDoor1 || door == GsxDoor.ServiceDoor2;
        }
    }
}
