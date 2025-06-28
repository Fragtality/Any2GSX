using CFIT.AppFramework.UI.ViewModels;
using System.Collections.Generic;

namespace Any2GSX.UI.Views.Audio
{
    public partial class ModelDeviceBlacklist(ModelAudio modelAudio) : ViewModelCollection<string, string>(modelAudio.Config.AudioDeviceBlacklist, (s) => s, (s) => !string.IsNullOrWhiteSpace(s))
    {
        protected virtual ModelAudio ModelAudio { get; } = modelAudio;
        public override ICollection<string> Source => ModelAudio.Config.AudioDeviceBlacklist;
        public virtual List<string> DeviceBlacklist => Source as List<string>;

        public override bool UpdateSource(string oldItem, string newItem)
        {
            try
            {
                int index = DeviceBlacklist.IndexOf(oldItem);
                if (IsUpdateAllowed(oldItem, newItem) && index >= 0)
                {
                    DeviceBlacklist[index] = newItem;
                    return true;
                }
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
