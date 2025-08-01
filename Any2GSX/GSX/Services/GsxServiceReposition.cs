﻿using Any2GSX.GSX.Menu;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.SimConnectLib.SimResources;

namespace Any2GSX.GSX.Services
{
    public class GsxServiceReposition(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Reposition;
        protected override ISimResourceSubscription SubStateVar => null;
        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(10, GsxConstants.MenuGate, true));
            sequence.Commands.Add(new(1, GsxConstants.MenuParkingSelect) { WaitReady = true });
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());
            sequence.Commands.Add(GsxMenuCommand.CreateReset());
            sequence.IgnoreGsxState = true;

            return sequence;
        }

        protected override void InitSubscriptions()
        {

        }

        protected override void DoReset()
        {

        }

        public override void FreeResources()
        {

        }

        protected override GsxServiceState GetState()
        {
            if (SequenceResult)
                return GsxServiceState.Completed;
            else
                return GsxServiceState.Callable;
        }

        protected override bool CheckCalled()
        {
            return SequenceResult;
        }
    }
}
