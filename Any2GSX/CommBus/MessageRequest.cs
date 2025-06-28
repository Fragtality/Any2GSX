using Any2GSX.PluginInterface.Interfaces;
using System.Text.Json;

namespace Any2GSX.CommBus
{
    public enum RequestType
    {
        CALL = 1,
        REGISTER = 2,
        UNREGISTER = 3,
        REMOVEALL = 4,
        PING = 5,
        RELAY = 6,
        CODE = 7,
        EFB = 8,
    };

    public class MessageRequest
    {
#pragma warning disable IDE1006
        public RequestType type { get; set; }
        public string @event { get; set; }
        public string data { get; set; }
        public BroadcastFlag flag { get; set; }
#pragma warning restore

        public static MessageRequest Create(RequestType type, string @event, string data = "", BroadcastFlag flag = BroadcastFlag.DEFAULT)
        {
            return new MessageRequest()
            {
                type = type,
                @event = @event,
                data = data,
                flag = flag
            };
        }

        public static MessageRequest Parse(string json)
        {
            return JsonSerializer.Deserialize<MessageRequest>(json);
        }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
