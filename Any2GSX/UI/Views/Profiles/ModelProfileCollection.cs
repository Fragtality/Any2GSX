using Any2GSX.AppConfig;
using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ValueConverter;
using CFIT.AppFramework.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Any2GSX.UI.Views.Profiles
{
    public partial class ModelProfileCollection() : ViewModelCollection<SettingProfile, ModelProfileItem>(AppService.Instance?.Config?.SettingProfiles ?? [], (i) => new(i))
    {
        public override ICollection<SettingProfile> Source => AppService.Instance?.Config?.SettingProfiles ?? [];
        public override Func<SettingProfile, bool> Validator => Check;

        protected bool Check(SettingProfile p)
        {
            return !string.IsNullOrWhiteSpace(p?.Name) && !Source.Any((s) => s.Name == p.Name);
        }

        protected override void InitializeMemberBindings()
        {
            base.InitializeMemberBindings();

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
                        AppService.Instance.UpdateSettingProfile(oldItem, newItem);
                        return true;
                    }
                    else if (!Source.Where(p => p.Name.Equals(newItem.Name, StringComparison.InvariantCultureIgnoreCase)).Any())
                    {
                        AppService.Instance.UpdateSettingProfile(oldItem, newItem);
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
            AppService.Instance.AddSettingProfile(item);
        }

        protected override bool RemoveSource(SettingProfile item)
        {
            return AppService.Instance.RemoveSettingProfile(item);
        }
    }
}
