using Any2GSX.AppConfig;
using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ValueConverter;
using CFIT.AppFramework.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Any2GSX.UI.Views.Profiles
{
    public partial class ModelProfileCollection() : ViewModelCollection<SettingProfile, ModelProfileItem>(AppService.Instance?.Config?.SettingProfiles ?? [], (i) => new(i), (p) => !string.IsNullOrWhiteSpace(p?.Name))
    {
        public override ICollection<SettingProfile> Source => AppService.Instance?.Config?.SettingProfiles ?? [];

        protected override void InitializeMemberBindings()
        {
            base.InitializeMemberBindings();

            CreateMemberBinding<bool, bool>(nameof(ModelProfileItem.IsReadOnly), new NoneConverter());
            CreateMemberBinding<string, string>(nameof(ModelProfileItem.Name), new NoneConverter(), new ValidationRuleString());
            CreateMemberBinding<string, string>(nameof(ModelProfileItem.PluginId), new NoneConverter(), new ValidationRuleString());
            CreateMemberBinding<string, string>(nameof(ModelProfileItem.ChannelFileId), new NoneConverter(), new ValidationRuleString());
            CreateMemberBinding<bool, bool>(nameof(ModelProfileItem.RunAutomationService), new NoneConverter());
            CreateMemberBinding<bool, bool>(nameof(ModelProfileItem.RunAudioService), new NoneConverter());
            CreateMemberBinding<bool, bool>(nameof(ModelProfileItem.PilotsDeckIntegration), new NoneConverter());
        }

        public override bool UpdateSource(SettingProfile oldItem, SettingProfile newItem)
        {
            try
            {
                if (Contains(oldItem))
                {
                    if (oldItem.Name.Equals(newItem?.Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        oldItem.Copy(newItem);
                        AppService.Instance.Config?.SettingProfiles?.Sort((x, y) => x.Name.CompareTo(y.Name));
                        return true;
                    }
                    else if (!Source.Where(p => p.Name.Equals(newItem.Name, StringComparison.InvariantCultureIgnoreCase)).Any())
                    {
                        oldItem.Copy(newItem);
                        AppService.Instance.Config?.SettingProfiles?.Sort((x, y) => x.Name.CompareTo(y.Name));
                        return true;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        protected override void AddSource(SettingProfile item)
        {
            base.AddSource(item);
            AppService.Instance.Config?.SettingProfiles?.Sort((x, y) => x.Name.CompareTo(y.Name));
        }
    }
}
