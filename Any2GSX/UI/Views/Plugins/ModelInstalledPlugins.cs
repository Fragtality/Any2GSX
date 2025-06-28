using Any2GSX.PluginInterface;
using CFIT.AppFramework.UI.ValueConverter;
using CFIT.AppFramework.UI.ViewModels;
using System.Collections.Generic;

namespace Any2GSX.UI.Views.Plugins
{
    public partial class ModelInstalledPlugins(ModelPlugins modelPlugins) : ViewModelCollection<PluginManifest, PluginManifest>((modelPlugins?.AppService?.PluginController?.Plugins as IDictionary<string, PluginManifest>)?.Values ?? [], (s) => s, (s) => s != null)
    {
        protected virtual ModelPlugins ModelPlugins { get; } = modelPlugins;
        public override ICollection<PluginManifest> Source => (ModelPlugins?.AppService?.PluginController?.Plugins as IDictionary<string, PluginManifest>)?.Values ?? [];

        protected override void InitializeMemberBindings()
        {
            base.InitializeMemberBindings();

            CreateMemberBinding<string, string>(nameof(PluginManifest.Id), new NoneConverter());
            CreateMemberBinding<string, string>(nameof(PluginManifest.Aircraft), new NoneConverter());
            CreateMemberBinding<bool, bool>(nameof(PluginManifest.Author), new NoneConverter());
            CreateMemberBinding<bool, bool>(nameof(PluginManifest.Version), new NoneConverter());
        }
    }
}
