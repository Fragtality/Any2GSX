using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Any2GSX.PluginInterface.Interfaces
{
    public interface IGsxMenu
    {
        public GsxMenuState MenuState { get; }
        public string MenuTitle { get; }
        public bool HasTitle { get; }
        public int MenuLineCount { get; }
        public List<string> MenuLines { get; }
        public string PathMenu { get; }

        public bool FirstReadyReceived { get; }
        public bool IsMenuReady { get; }
        public bool IsGateMenu { get; }
        public bool IsGateSelectionMenu { get; }
        public bool IsOperatorMenu { get; }
        public bool IsSequenceActive { get; }
        public bool DeIceQuestionAnswered { get; }
        public bool MenuOpenRequesting { get; }
        public bool FollowMeAnswered { get; }
        public bool SuppressMenuRefresh { get; }

        public event Action<IGsxMenu> OnMenuReady;
        public event Action<IGsxMenu> OnMenuReceived;
        public event Action<string> MenuTitleChanged;

        public void AddMenuCallback(string title, Func<IGsxMenu, Task> callback);
        public void RemoveMenuCallback(string title);

        public Task<bool> Open(bool waitReady = false);
        public Task<bool> OpenHide();
        public Task Select(int number, bool waitReady = true, bool openMenu = false, int hide = 0);
    }
}
