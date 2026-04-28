using Any2GSX.AppConfig;
using System;

namespace Any2GSX.UI.Views.Profiles
{
    public partial class ModelProfileItem(SettingProfile source) : ModelBase<SettingProfile>(source, AppService.Instance)
    {
        protected override void InitializeModel()
        {

        }

        public override string ToString()
        {
            return Source.ToString();
        }

        public virtual bool IsReadOnly => Source?.IsReadOnly == true;
        public virtual bool IsEditAllowed => !IsReadOnly;
        public virtual bool IsActive => AppService?.ProfileName?.Equals(Name, StringComparison.InvariantCultureIgnoreCase) == true;
        public virtual bool CanActivate => !IsActive;
        public virtual string Name { get => Source?.Name ?? ""; set { } }
        public virtual string PluginId { get => Source?.PluginId ?? ""; set => SetPlugin(value); }
        public virtual string ChannelFileId { get => Source?.ChannelFileId ?? ""; set => SetChannel(value); }
        public virtual bool RunAutomationService { get => Source?.RunAutomationService ?? false; set { } }
        public virtual bool RunAudioService { get => Source?.RunAudioService ?? false; set { } }
        public virtual bool PilotsDeckIntegration { get => Source?.PilotsDeckIntegration ?? false; set { } }

        protected virtual void SetPlugin(string pluginId)
        {
            if (Source.PluginId == pluginId || string.IsNullOrWhiteSpace(pluginId))
                return;

            Source.PluginId = pluginId;
            SaveConfig();
            if (IsActive)
                AppService.NotifyPluginCapabilitiesChanged();
            AppService.NotifyProfileCollectionChanged();
        }

        protected virtual void SetChannel(string channelId)
        {
            if (Source.ChannelFileId == channelId || string.IsNullOrWhiteSpace(channelId))
                return;

            Source.ChannelFileId = channelId;
            SaveConfig();
            if (IsActive)
                AppService.NotifyAircraftChannelsChanged();
        }

        public virtual void SetFeature(bool? toggle, string propertyName)
        {
            SetModelValue<bool>(toggle == true, null, null, propertyName);
        }
    }
}
