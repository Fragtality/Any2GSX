using System.Collections.Generic;

namespace Any2GSX.PluginInterface
{
    public enum PluginSettingType
    {
        Bool = 1,
        Integer = 2,
        Number = 3,
        String = 4,
        Enum = 5,
    }

    public class PluginSetting
    {
        public virtual string Key { get; set; }
        public virtual PluginSettingType Type { get; set; }
        public virtual object DefaultValue { get; set; }
        public virtual Dictionary<int, string> EnumValues { get; set; } = [];
        public virtual string Description { get; set; }
    }
}
