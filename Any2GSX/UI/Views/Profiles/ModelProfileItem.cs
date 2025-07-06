using Any2GSX.AppConfig;

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
        public virtual string Name { get => Source.Name; set => SetModelValue<string>(value); }
        public virtual string PluginId { get => Source.PluginId; set => SetModelValue<string>(value); }
        public virtual string ChannelFileId { get => Source.ChannelFileId; set => SetModelValue<string>(value); }
        public virtual bool RunAutomationService { get => Source.RunAutomationService; set => SetModelValue<bool>(value); }
        public virtual bool RunAudioService { get => Source.RunAudioService; set => SetModelValue<bool>(value); }
        public virtual bool PilotsDeckIntegration { get => Source.PilotsDeckIntegration; set => SetModelValue<bool>(value); }
    }
}
