using Any2GSX.AppConfig;
using CFIT.AppLogger;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace Any2GSX.CommBus
{
    public class ApiController
    {
        public virtual HttpListener HttpListener { get; }
        public virtual bool IsExecutionAllowed { get; set; } = true;
        public virtual int Port { get; protected set; } = 0;
        protected virtual Config Config => AppService.Instance?.Config;

        public ApiController()
        {
            HttpListener = new();
        }

        public virtual void Stop()
        {
            IsExecutionAllowed = false;
        }

        protected virtual bool StartListener(int port)
        {
            try
            {
                HttpListener.Prefixes.Clear();
                HttpListener.Prefixes.Add($"http://localhost:{port}/");
                HttpListener.Start();
                Port = port;
                return true;
            }
            catch (Exception ex)
            {
                if (Config.LogLevel == LogLevel.Verbose)
                    Logger.LogException(ex);
                Logger.Debug($"Exception while starting ApiTask");
            }

            return false;
        }

        public async virtual void Run()
        {
            Port = 0;
            for (int i = 0; i < Config.PortRange; i++)
            {
                if (StartListener(Config.PortBase + i))
                    break;
            }
            if (Port == 0)
            {
                Logger.Error($"HttpListener could not be started!");
                MessageBox.Show("The HTTP Listener could not be started but is required to connect to the CommBus Module.\r\nAny2GSX will exit now. If the Problem continues, try a Reboot of your PC.", $"HTTP Listener could not be started!", MessageBoxButton.OK, MessageBoxImage.Error);
                throw new Exception($"HttpListener could not be started!");
            }
            else
            {
                AppService.Instance.CommBus.Port = Port;
                Logger.Debug($"Using Port {Port} to listen for CommBus Messages");
            }

            Logger.Information("ApiTask started");
            bool first = true;
            while (!AppService.Instance.Token.IsCancellationRequested && IsExecutionAllowed)
            {
                try
                {
                    HttpListenerContext context = await HttpListener.GetContextAsync().WaitAsync(AppService.Instance.Token);
                    if (first)
                    {
                        Logger.Debug($"First Message from CommBus Module received");
                        first = false;
                    }

                    if (context.Request.RawUrl.StartsWith("/v1/"))
                        HandleRequestV1(context);
                    else
                    {
                        Logger.Warning($"Received unknown Request: {context.Request.RawUrl} (Method: {context.Request.HttpMethod})");
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }

                    context?.Response?.Close();
                }
                catch (Exception ex)
                {
                    if (ex is not OperationCanceledException && ex is not TaskCanceledException)
                        Logger.LogException(ex);
                }
            }

            try { HttpListener?.Close(); } catch { }
            Logger.Information("ApiTask ended");
        }

        protected virtual void HandleRequestV1(HttpListenerContext context)
        {
            string url = context.Request.RawUrl.Replace("/v1/", "");
            Logger.Verbose($"Handling Request: {context.Request.HttpMethod} @ {context.Request.RawUrl}");
            if (url.StartsWith("pushevent"))
                HandlePushEventV1(context, url.Replace("pushevent", ""));
            else if (url.StartsWith("ping-reply"))
                AppService.Instance.CommBus.IsPingReceived = true;
            else
            {
                Logger.Warning($"Received unknown {context.Request.HttpMethod} Request: {context.Request.RawUrl}");
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
            Logger.Verbose($"Result: {context.Response.StatusCode}");
        }

        protected virtual void HandlePushEventV1(HttpListenerContext context, string url)
        {
            try
            {
                using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                var msg = MessageReceive.Parse(reader.ReadToEnd());
                Logger.Verbose($"Received Push Event: {msg.@event}");
                AppService.Instance.CommBus.PushEvent(msg.@event, msg.data);
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}
