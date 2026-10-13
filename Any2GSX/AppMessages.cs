using CFIT.AppFramework.MessageService;

namespace Any2GSX
{
    public class MsgAppSessionStarting() : AppMessage();
    public class MsgAppSessionStopping() : AppMessage();
    public class MsgAppSessionStarted() : AppMessage();
    public class MsgAppSessionStopped() : AppMessage();
}
