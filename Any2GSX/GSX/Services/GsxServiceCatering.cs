using Any2GSX.GSX.Menu;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.SimConnectLib.SimResources;

namespace Any2GSX.GSX.Services
{
    public class GsxServiceCatering(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Catering;
        public virtual ISimResourceSubscription SubCaterService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubCaterService;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Select(2, GsxConstants.MenuGate));
            sequence.Commands.Add(GsxMenuCommand.Operator(false));

            return sequence;
        }

        protected override GsxMenuSequence InitCancelSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Select(2, GsxConstants.MenuGate));

            return sequence;
        }

        public override void InitSubscriptions()
        {
            SubCaterService = SimStore.AddVariable(GsxConstants.VarServiceCatering);
            SubCaterService?.OnReceived += OnStateChange;
        }

        protected override void DoReset()
        {

        }

        public override void FreeResources()
        {
            SubCaterService?.OnReceived -= OnStateChange;

            SimStore.Remove(GsxConstants.VarServiceCatering);
        }
    }
}
