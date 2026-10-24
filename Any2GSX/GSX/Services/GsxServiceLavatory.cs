using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.PluginInterface.Interfaces.GSX;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Services
{
    public class GsxServiceLavatory() : GsxService()
    {
        public override GsxServiceType Type => GsxServiceType.Lavatory;
        protected override int MenuSelectionService => 3;
        protected override string StateVarName => GsxConstants.VarServiceLavatory;
        protected override List<string> ServiceVariables => [];
        public override bool HasStairConflict => Controller.HasGateStair && Controller.Aircraft.HasRearStair() && Controller.IsLavatoryOnStairSide && Controller.ApiData?.HandlerData?.aircraft?.HasLavatoryConflict() == true;

        protected override void DoInit()
        {

        }

        protected override Task DoReset(bool resetVariable)
        {
            return Task.CompletedTask;
        }

        protected override void DoRelease()
        {

        }
    }
}
