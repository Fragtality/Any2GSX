using Any2GSX.Plugins;
using CFIT.AppFramework.UI.ValueConverter;
using CFIT.AppFramework.UI.ViewModels;
using System.Collections.Generic;

namespace Any2GSX.UI.Views.Plugins
{
    public partial class ModelRepoProfiles(ModelPlugins modelPlugins) : ViewModelCollection<ProfileManifest, ProfileManifest>((modelPlugins?.PluginRepo?.Profiles as IDictionary<string, ProfileManifest>).Values ?? [], (s) => s, (s) => s != null)
    {
        protected virtual ModelPlugins ModelPlugins { get; } = modelPlugins;
        public override ICollection<ProfileManifest> Source => (ModelPlugins?.PluginRepo?.Profiles as IDictionary<string, ProfileManifest>).Values ?? [];

        protected override void InitializeMemberBindings()
        {
            base.InitializeMemberBindings();

            CreateMemberBinding<string, string>(nameof(ProfileManifest.Name), new NoneConverter());
            CreateMemberBinding<string, string>(nameof(ProfileManifest.Aircraft), new NoneConverter());
            CreateMemberBinding<bool, bool>(nameof(ProfileManifest.Author), new NoneConverter());
            CreateMemberBinding<bool, bool>(nameof(ProfileManifest.Version), new NoneConverter());
        }
    }
}
