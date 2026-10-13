using Any2GSX.GSX.Automation;
using CFIT.AppFramework.UI.ViewModels;
using System.Collections.Generic;

namespace Any2GSX.UI.Views.Automation
{
    public partial class ModelDepartureServices(ModelAutomation modelAutomation) : ViewModelCollection<ServiceConfig, ModelServiceConfig>(modelAutomation?.Source?.DepartureServices?.Values, (s) => new(s, modelAutomation), (s) => s != null)
    {
        protected virtual ModelAutomation ModelAutomation { get; } = modelAutomation;
        public override ICollection<ServiceConfig> Source => ModelAutomation?.Source?.DepartureServices?.Values;
        public virtual SortedDictionary<int, ServiceConfig> DepartureServices => ModelAutomation.Source.DepartureServices;
        public virtual bool WasMoved => ToIndex != -1;
        public virtual int ToIndex { get; set; } = -1;

        public virtual void MoveItem(int fromIndex, int step)
        {
            int toIndex = fromIndex + step;
            if (fromIndex < 0 || fromIndex >= DepartureServices.Count || toIndex < 0 || toIndex >= DepartureServices.Count)
                return;

            var temp = DepartureServices[toIndex];
            DepartureServices[toIndex] = DepartureServices[fromIndex];
            DepartureServices[fromIndex] = temp;
            ToIndex = toIndex;
            NotifyCollectionChanged();
        }
    }
}
