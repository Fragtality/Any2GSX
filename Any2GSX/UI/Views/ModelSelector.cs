using Any2GSX.AppConfig;

namespace Any2GSX.UI.Views
{
    public partial class ModelSelector(AppService appService) : ModelBase<SettingProfile>(appService?.SettingProfile, appService)
    {
        protected override void InitializeModel()
        {
            InitializeMessageService();
        }
    }
}
