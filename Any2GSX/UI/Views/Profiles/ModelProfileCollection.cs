using Any2GSX.AppConfig;
using CFIT.AppFramework.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Any2GSX.UI.Views.Profiles
{
    public partial class ModelProfileCollection() : ViewModelCollection<SettingProfile, SettingProfile>(AppService.Instance?.Config?.SettingProfiles ?? [], (i) => i, (p) => !string.IsNullOrWhiteSpace(p?.Name))
    {
        public override ICollection<SettingProfile> Source => AppService.Instance?.Config?.SettingProfiles ?? [];

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
