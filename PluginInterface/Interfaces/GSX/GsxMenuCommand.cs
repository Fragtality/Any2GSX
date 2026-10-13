using System.Collections.Generic;

namespace Any2GSX.PluginInterface.Interfaces.GSX
{
    public class GsxMenuCommand(int parameter, GsxMenuCommandType type = GsxMenuCommandType.Select, string title = "", bool waitRdy = true, string[] textSelect = null)
    {
        public int Parameter { get; } = parameter;
        public string Title { get; } = title;
        public bool ExcludeTitle { get; set; } = false;
        public bool HasTitle => !string.IsNullOrWhiteSpace(Title);
        public bool WaitReady { get; } = waitRdy;
        public bool HasTextSelect => TextSelect?.Count > 0;
        public List<string> TextSelect { get; set; } = textSelect != null ? new(textSelect) : [];
        public GsxMenuCommandType Type { get; } = type;
        public GsxMenuCommandOperatorType OperatorType => Type == GsxMenuCommandType.Operator ? (GsxMenuCommandOperatorType)Parameter : GsxMenuCommandOperatorType.Unknown;

        public static GsxMenuCommand Open()
        {
            return new GsxMenuCommand(0, GsxMenuCommandType.Open, "", true);
        }

        public static GsxMenuCommand Select(int line, string title = "", string[] textSelect = null, bool waitRdy = true)
        {
            return new GsxMenuCommand(line, GsxMenuCommandType.Select, title, waitRdy, textSelect);
        }

        public static GsxMenuCommand Wait(int factor = 0)
        {
            return new GsxMenuCommand(factor, GsxMenuCommandType.Wait);
        }

        public static GsxMenuCommand Operator(GsxMenuCommandOperatorType type)
        {
            return new GsxMenuCommand((int)type, GsxMenuCommandType.Operator, "", true);
        }

        public static GsxMenuCommand Close(string title = "", bool waitRdy = false)
        {
            return new GsxMenuCommand(0, GsxMenuCommandType.Close, title, waitRdy);
        }

        public static GsxMenuCommand Disable(string title = "", bool waitRdy = false)
        {
            return new GsxMenuCommand(0, GsxMenuCommandType.Disable, title, waitRdy);
        }

        public static GsxMenuCommand Service(GsxServiceType type, bool waitRdy = false)
        {
            return new GsxMenuCommand((int)type, GsxMenuCommandType.Service, "", waitRdy);
        }
    }
}
