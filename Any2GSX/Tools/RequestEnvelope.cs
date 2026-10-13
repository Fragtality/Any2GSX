using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Any2GSX.Tools
{
    public class RequestEnvelope
    {
        public HttpListenerContext Context { get; }
        public HttpListenerRequest Request => Context?.Request;
        public string RequestPath { get; }
        public List<string> PathComponents { get; }
        public string Version => PathComponents?.FirstOrDefault() ?? "";
        public string Endpoint => PathComponents?.Count > 1 ? PathComponents!?[1] ?? "" : "";
        public HttpListenerResponse Response => Context?.Response;
        public bool Handled { get; protected set; } = false;
        public JsonNode JsonData { get; protected set; } = JsonValue.Create(JsonValueKind.Object);

        public RequestEnvelope(HttpListenerContext context)
        {
            Context = context;
            RequestPath = Request?.Url?.GetComponents(UriComponents.Path, UriFormat.SafeUnescaped) ?? "";
            PathComponents = [.. RequestPath?.Split('/') ?? []];
        }

        public virtual JsonNode ParseData()
        {
            if (Request.HasEntityBody)
            {
                using var reader = new StreamReader(Request.InputStream, Request.ContentEncoding);
                JsonData = JsonNode.Parse(reader.ReadToEnd());
            }
            return JsonData;
        }

        public virtual void SetResponseCode(HttpStatusCode code, string message = null)
        {
            Response.StatusCode = (int)code;
            if (!string.IsNullOrWhiteSpace(message))
                Response.StatusDescription = message;

            Handled = true;
        }

        public virtual void SetOk()
        {
            Response.StatusCode = (int)HttpStatusCode.OK;
            Handled = true;
        }

        public virtual void SetResonseContent(object obj, HttpStatusCode? code = null)
        {
            SetResonseContent(JsonSerializer.Serialize(obj));
        }

        public virtual void SetResonseContent(Dictionary<string, object> dict, HttpStatusCode? code = null)
        {
            SetResonseContent(JsonSerializer.Serialize(dict));
        }

        public virtual void SetResonseContent(string json, HttpStatusCode? code = null)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            Response.ContentType = "application/json";
            Response.ContentEncoding = Encoding.UTF8;
            Response.ContentLength64 = buffer.Length;
            Response.StatusCode = (int)(code ?? HttpStatusCode.OK);
            Handled = true;

            using var output = Response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
        }

        public virtual void Close()
        {
            Response.Close();
        }
    }
}
