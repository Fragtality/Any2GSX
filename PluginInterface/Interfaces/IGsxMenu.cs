using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Any2GSX.PluginInterface.Interfaces
{
    public interface IGsxMenu
    {
        public string PathMenu { get; }
        public bool IsInitialized { get; }
        public GsxMenuState MenuState { get; }
        public string TextMenuState { get; }
        public string MenuTitle { get; }
        public bool HasTitle { get; }
        public int MenuLineCount { get; }
        public List<string> MenuLines { get; }

        public bool IsReady { get; }
        public bool ReadyReceived { get; }
        public bool FirstReadyReceived { get; }
        public bool IsSequenceActive { get; }
        public bool IsToolbarEnabled { get; }
        public bool IsCommandActive { get; }
        public bool MenuUpdatesBlocked { get; }
        public bool MenuCommandsAllowed { get; }
        public bool WasOperatorPreferred { get; }
        public bool WasOperatorHandlingSelected { get; }
        public bool WasOperatorCateringSelected { get; }
        public bool IsGateMenu { get; }
        public bool IsDeicePad { get; }
        public bool IsSelectGateMenu { get; }
        public bool IsChangeGateMenu { get; }
        public bool IsOperatorMenu { get; }

        public double LastMenuSelection { get; }
        public DateTime LastSelectionTime { get; }
        public DateTime LastTimeout { get; }
        public bool DeiceGateQuestionAnswered { get; }


        public event Func<string, Task> MenuTitleChanged;
        public event Func<IGsxMenu, Task> OnMenuReady;
        public event Func<IGsxMenu, Task> OnMenuReceived;

        public void AddMenuCallback(string title, Func<IGsxMenu, Task> callback);
        public void RemoveMenuCallback(string title);

        public bool MatchTitle(string match);
        public bool MatchMenuLine(int line, string match);
        public bool MatchMenuLine(double line, string match);
        public string GetMenuLine(int line);
        public string GetMenuLine(double line);
        public int FindMenuLine(string match);

        public Task<bool> WaitMenuReady(int timeout = 0);
        public Task<bool> RunCommand(GsxMenuCommand command, bool enableMenu);
    }
}
