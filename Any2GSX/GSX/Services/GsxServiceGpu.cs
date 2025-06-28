﻿using Any2GSX.GSX.Menu;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.SimConnectLib.SimResources;

namespace Any2GSX.GSX.Services
{
    public class GsxServiceGpu(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.GPU;
        public virtual ISimResourceSubscription SubGpuService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubGpuService;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(8, GsxConstants.MenuGate, true));
            sequence.Commands.Add(new(1, GsxConstants.MenuAdditionalServices) { WaitReady = true });
            sequence.Commands.Add(GsxMenuCommand.CreateOperator());
            sequence.Commands.Add(GsxMenuCommand.CreateReset());

            return sequence;
        }

        protected override void InitSubscriptions()
        {
            SubGpuService = SimStore.AddVariable(GsxConstants.VarServiceGpu);
            SubGpuService.OnReceived += OnStateChange;
        }

        protected override void DoReset()
        {

        }

        public override void FreeResources()
        {
            SubGpuService.OnReceived -= OnStateChange;

            SimStore.Remove(GsxConstants.VarServiceGpu);
        }
    }
}
