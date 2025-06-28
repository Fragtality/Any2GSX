using CFIT.AppFramework.UI.ViewModels;
using System.Collections.Generic;

namespace Any2GSX.UI.Views.Audio
{
    public partial class ModelAudioChannels(ModelAudio modelAudio) : ViewModelCollection<string, string>(modelAudio?.Source?.ChannelIds ?? [], (s) => s, (s) => s != null)
    {
        protected virtual ModelAudio ModelAudio { get; } = modelAudio;
        public override ICollection<string> Source => ModelAudio?.Source?.ChannelIds ?? [];
    }
}
