using System.Net;

namespace Any2GSX.Tools
{
    public class RequestResult
    {
        public virtual HttpStatusCode Code { get; set; } = HttpStatusCode.OK;
        public virtual string Message { get; set; } = string.Empty;

        public RequestResult() { }

        public RequestResult(HttpStatusCode code, string message)
        {
            Code = code;
            Message = message;
        }
    }
}
