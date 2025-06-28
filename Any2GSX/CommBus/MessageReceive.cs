using System.Text.Json;

namespace Any2GSX.CommBus
{
    public class MessageReceive
    {
#pragma warning disable IDE1006
        public string @event { get; set; }
        public string data { get; set; }
#pragma warning restore

        public static MessageReceive Create(string @event, string data)
        {
            return new MessageReceive()
            {
                @event = @event,
                data = data,
            };
        }

        public static MessageReceive Parse(string json)
        {
            return JsonSerializer.Deserialize<MessageReceive>(json);
        }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
