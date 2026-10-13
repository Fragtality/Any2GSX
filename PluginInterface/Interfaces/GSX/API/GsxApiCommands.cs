using Any2GSX.PluginInterface.Interfaces.GSX.API.Messages;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Any2GSX.PluginInterface.Interfaces.GSX.API
{
#pragma warning disable IDE1006

    public class GsxApiMsgSubscribe(string type, List<string> channels)
    {
        public virtual string type { get; } = type;
        public virtual string id => type;
        public virtual List<string> channels { get; } = channels ?? ["state", "prompts", "toasts"];

        public static GsxApiMsgSubscribe Subscribe(List<string> channels = null)
        {
            return new GsxApiMsgSubscribe("subscribe", channels);
        }

        public static GsxApiMsgSubscribe Unsubscribe(List<string> channels = null)
        {
            return new GsxApiMsgSubscribe("unsubscribe", channels);
        }
    }

    public abstract class GsxApiCommand
    {
        public virtual string type { get; } = "command";
        [JsonIgnore]
        public static ulong IdCounter { get; set; } = 0;
        public virtual string id { get; protected set; }
        public abstract string verb { get; }
        public virtual object args { get; }
        [JsonIgnore]
        public virtual GsxMessageResult result { get; protected set; } = null;
        [JsonIgnore]
        public virtual bool HasResult => result != null;
        [JsonIgnore]
        public virtual bool HasError => result?.HasError == true;

        public GsxApiCommand(object args)
        {
            this.args = args;
            this.id = $"{verb}.{IdCounter++}";
        }

        public static T Create<T>(object args) where T : GsxApiCommand
        {
            return (T)Activator.CreateInstance(typeof(T), args);
        }

        public virtual void SetResult(bool ok, string code = null)
        {
            result = GsxMessageResult.Create(ok, id, code);
        }

        public virtual void SetResult(GsxMessageResult result)
        {
            this.result = result;
        }

        public virtual void SetError(string code)
        {
            SetResult(false, code);
        }

    }

    public class GsxApiCmdGetState(object args) : GsxApiCommand(args)
    {
        public override string verb => "state.get";
    }

    public class GsxApiCmdCallService(object args) : GsxApiCommand(args)
    {
        public override string verb => "service.trigger";
    }

    public class GsxApiCmdMenuOpen(object args) : GsxApiCommand(args)
    {
        public override string verb => "menu.open";
    }

    public class GsxApiCmdMenuClose(object args) : GsxApiCommand(args)
    {
        public override string verb => "menu.close";
    }

    public class GsxApiCmdMenuSelect(object args) : GsxApiCommand(args)
    {
        public override string verb => "menu.pick";
    }

    public class GsxApiCmdMenuSearch(object args) : GsxApiCommand(args)
    {
        public override string verb => "menu.search";
    }

    public class GsxApiCmdSetSetting(object args) : GsxApiCommand(args)
    {
        public override string verb => "settings.set";
    }

    public class GsxApiCmdSetHandler(object args) : GsxApiCommand(args)
    {
        public override string verb => "handler.set";
    }

    public class GsxApiCmdSelectGate(object args) : GsxApiCommand(args)
    {
        public override string verb => "gate.select";
    }

    public class GsxApiCmdInputSubmit(object args) : GsxApiCommand(args)
    {
        public override string verb => "input.submit";
    }

    public class GsxApiCmdInputCancel(object args) : GsxApiCommand(args)
    {
        public override string verb => "input.cancel";
    }

    public class GsxApiCmdRunCommand(object args) : GsxApiCommand(args)
    {
        public override string verb => "command.run";
    }
#pragma warning restore IDE1006
}
