using Any2GSX.Aircraft;
using CFIT.AppFramework.UI.ValueConverter;
using CFIT.AppFramework.UI.ViewModels;
using System.Collections.Generic;

namespace Any2GSX.UI.Views.Plugins
{
    public partial class ModelInstalledChannels(ModelPlugins modelPlugins) : ViewModelCollection<AircraftChannels, AircraftChannels>((modelPlugins?.AppService?.PluginController?.Channels as IDictionary<string, AircraftChannels>)?.Values ?? [], (s) => s, (s) => s != null)
    {
        protected virtual ModelPlugins ModelPlugins { get; } = modelPlugins;
        public override ICollection<AircraftChannels> Source => (ModelPlugins?.AppService?.PluginController?.Channels as IDictionary<string, AircraftChannels>)?.Values ?? [];

        protected override void InitializeMemberBindings()
        {
            base.InitializeMemberBindings();

            CreateMemberBinding<string, string>(nameof(AircraftChannels.Id), new NoneConverter());
            CreateMemberBinding<string, string>(nameof(AircraftChannels.Aircraft), new NoneConverter());
            CreateMemberBinding<bool, bool>(nameof(AircraftChannels.Author), new NoneConverter());
            CreateMemberBinding<bool, bool>(nameof(AircraftChannels.Version), new NoneConverter());
        }
    }
}
