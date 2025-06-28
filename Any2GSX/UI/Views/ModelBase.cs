using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.Audio;
using Any2GSX.GSX;
using CFIT.AppFramework.UI.ViewModels;
using CFIT.SimConnectLib;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Any2GSX.UI.Views
{
    public abstract partial class ModelBase<TObject>(TObject source, AppService appService) : ViewModelBase<TObject>(source) where TObject : class
    {
        public virtual AppService AppService { get; } = appService;
        public virtual Config Config => AppService.Config;
        public virtual SimConnectController SimConnectController => AppService.SimService.Controller;
        public virtual SimConnectManager SimConnect => AppService.SimConnect;
        public virtual GsxController GsxController => AppService.GsxController;
        public virtual AudioController AudioController => AppService.AudioController;
        public virtual AircraftController AircraftController => AppService.AircraftController;
        public virtual SettingProfile SettingProfile => AppService.SettingProfile;
        public virtual bool InhibitConfigSave { get; set; } = false;

        public virtual void SaveConfig()
        {
            if (!InhibitConfigSave)
                Config.SaveConfiguration();
        }

        public virtual void SetModelValue<T>(T value, Func<T, ValidationContext, ValidationResult> validator = null, Action<T, T> callback = null, [CallerMemberName] string propertyName = null!)
        {
            SetSourceValue(value, validator, callback, propertyName);
            SaveConfig();
        }
    }
}
