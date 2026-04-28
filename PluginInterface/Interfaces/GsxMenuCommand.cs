using System.Collections.Generic;

namespace Any2GSX.PluginInterface.Interfaces
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
        public bool IsHandlingOperator => Type == GsxMenuCommandType.Operator && Parameter == 1;

        public static GsxMenuCommand Open()
        {
            return new GsxMenuCommand(0, GsxMenuCommandType.Open, "", true);
        }

        public static GsxMenuCommand State(GsxMenuState state, string title = "")
        {
            return new GsxMenuCommand((int)state, GsxMenuCommandType.State, title, false);
        }

        public static GsxMenuCommand Select(int line, string title = "", string[] textSelect = null, bool waitRdy = true)
        {
            return new GsxMenuCommand(line, GsxMenuCommandType.Select, title, waitRdy, textSelect);
        }

        public static GsxMenuCommand Wait(int factor = 0)
        {
            return new GsxMenuCommand(factor, GsxMenuCommandType.Wait);
        }

        public static GsxMenuCommand Operator(bool handling = true)
        {
            return new GsxMenuCommand(handling ? 1 : 0, GsxMenuCommandType.Operator, "", true);
        }
    }
}
