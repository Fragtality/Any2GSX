using Any2GSX.AppConfig;
using CFIT.AppFramework.UI.ValueConverter;
using CFIT.AppFramework.UI.ViewModels;
using System.Collections.Generic;

namespace Any2GSX.UI.Views.Profiles
{
    public partial class ModelMatchingCollection(SettingProfile profile) : ViewModelCollection<ProfileMatching, ProfileMatching>(profile?.ProfileMatches ?? [], (i) => i, (p) => !string.IsNullOrWhiteSpace(p?.MatchString))
    {
        public virtual SettingProfile Profile { get; protected set; } = profile;
        public override ICollection<ProfileMatching> Source => Profile?.ProfileMatches ?? [];

        protected override void InitializeMemberBindings()
        {
            base.InitializeMemberBindings();

            CreateMemberBinding<MatchData, MatchData>(nameof(ProfileMatching.MatchData), new NoneConverter());
            CreateMemberBinding<MatchOperation, MatchOperation>(nameof(ProfileMatching.MatchOperation), new NoneConverter());
            CreateMemberBinding<string, string>(nameof(ProfileMatching.MatchString), new NoneConverter());
        }

        public virtual void ChangeProfile(SettingProfile profile)
        {
            Profile = profile;
            NotifyCollectionChanged();
        }
    }
}
