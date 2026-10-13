using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.PluginInterface.Interfaces.GSX;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Services
{
    public class GsxServiceWater() : GsxService()
    {
        public override GsxServiceType Type => GsxServiceType.Water;
        protected override int MenuSelectionService => 4;
        protected override string StateVarName => GsxConstants.VarServiceWater;
        protected override List<string> ServiceVariables => [];
        public override bool HasStairConflict => Controller.HasGateStair && Controller.Aircraft.HasRearStair() && Controller.IsWaterOnStairSide && Controller.ApiData?.HandlerData?.aircraft?.HasWaterConflict() == true;

        protected override void DoInit()
        {

        }

        protected override Task DoReset()
        {
            return Task.CompletedTask;
        }

        protected override void DoRelease()
        {

        }
    }
}
