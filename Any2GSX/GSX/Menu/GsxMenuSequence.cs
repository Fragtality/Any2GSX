using Any2GSX.PluginInterface.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Any2GSX.GSX.Menu
{
    public class GsxMenuSequence(List<GsxMenuCommand> commands = null)
    {
        public virtual List<GsxMenuCommand> Commands { get; } = commands ?? [];
        public virtual bool IsExecuting { get; set; } = false;
        public virtual bool IsSuccess { get; set; } = false;
        public virtual bool IgnoreGsxState { get; set; } = false;
        public virtual bool HasOperatorSelection => Commands?.Any(c => c.Type == GsxMenuCommandType.Operator) ?? false;
        public virtual bool IsHandlingOperator => Commands?.FirstOrDefault(c => c.Type == GsxMenuCommandType.Operator)?.IsHandlingOperator == true;
        public virtual Func<bool> EnableMenuCheck { get; set; } = () => false;
        public virtual bool EnableMenu => EnableMenuCheck?.Invoke() == true;
        public virtual Func<bool> ResetMenuCheck { get; set; } = () => true;
        public virtual bool ResetMenu => ResetMenuCheck?.Invoke() == true;
        public virtual Func<bool> EnableMenuAfterResetCheck { get; set; } = () => false;
        public virtual bool EnableMenuAfterReset => EnableMenuAfterResetCheck?.Invoke() == true;

        public virtual void Reset()
        {
            IsSuccess = false;
            IsExecuting = false;
        }
    }
}
