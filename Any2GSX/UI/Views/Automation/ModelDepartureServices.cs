using Any2GSX.GSX;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppFramework.UI.ValueConverter;
using CFIT.AppFramework.UI.ViewModels;
using System.Collections.Generic;

namespace Any2GSX.UI.Views.Automation
{
    public partial class ModelDepartureServices(ModelAutomation modelAutomation) : ViewModelCollection<ServiceConfig, ServiceConfig>(modelAutomation?.Source?.DepartureServices?.Values, (s) => s, (s) => s != null)
    {
        protected virtual ModelAutomation ModelAutomation { get; } = modelAutomation;
        public override ICollection<ServiceConfig> Source => ModelAutomation?.Source?.DepartureServices?.Values;
        public virtual SortedDictionary<int, ServiceConfig> DepartureServices => ModelAutomation.Source.DepartureServices;

        protected override void InitializeMemberBindings()
        {
            base.InitializeMemberBindings();

            CreateMemberBinding<GsxServiceActivation, GsxServiceActivation>(nameof(ServiceConfig.ServiceActivation), new NoneConverter());
            CreateMemberBinding<GsxServiceConstraint, GsxServiceConstraint>(nameof(ServiceConfig.ServiceConstraint), new NoneConverter());
            CreateMemberBinding<bool, bool>(nameof(ServiceConfig.CallOnCargo), new NoneConverter());
        }

        public override bool UpdateSource(ServiceConfig oldItem, ServiceConfig newItem)
        {
            try
            {
                if (oldItem.ServiceType == newItem.ServiceType)
                {
                    oldItem.ServiceActivation = newItem.ServiceActivation;
                    oldItem.MinimumFlightDuration = newItem.MinimumFlightDuration;
                    oldItem.ServiceConstraint = newItem.ServiceConstraint;
                    oldItem.CallOnCargo = newItem.CallOnCargo;
                    return true;
                }
            }
            catch { }

            return false;
        }

        public virtual void MoveItem(int fromIndex, int step)
        {
            int toIndex = fromIndex + step;
            if (fromIndex < 0 || fromIndex >= DepartureServices.Count || toIndex < 0 || toIndex >= DepartureServices.Count)
                return;

            var temp = DepartureServices[toIndex];
            DepartureServices[toIndex] = DepartureServices[fromIndex];
            DepartureServices[fromIndex] = temp;
            NotifyCollectionChanged();
        }
    }
}