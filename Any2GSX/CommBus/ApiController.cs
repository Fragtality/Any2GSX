using Any2GSX.AppConfig;
using Any2GSX.GSX;
using CFIT.AppFramework.Services;
using CFIT.AppLogger;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace Any2GSX.CommBus
{
    public class ApiController(Config config) : ServiceController<Any2GSX, AppService, Config, Definition>(config)
    {
        public virtual HttpListener HttpListener { get; } = new();
        public virtual int Port { get; protected set; } = 0;

        protected override Task DoInit()
        {
            return Task.CompletedTask;
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

        protected override async Task DoRun()
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
                MessageBox.Show("The HTTP Listener could not be started but is required to connect to the CommBus Module.\r\nAny2GSX will exit now. If the Problem continues, try a Reboot of your PC (or another 'PortBase' in the AppConfig.json).", $"HTTP Listener could not be started!", MessageBoxButton.OK, MessageBoxImage.Error);
                throw new Exception($"HttpListener could not be started!");
            }
            else
            {
                AppService.Instance.CommBus.Port = Port;
                Logger.Debug($"Using Port {Port} to listen for CommBus Messages");
            }

            Logger.Information("API Controller started");
            bool first = true;
            while (IsExecutionAllowed)
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
            Logger.Information("API Controller ended");
        }

        protected override Task DoCleanup()
        {
            try { HttpListener?.Close(); } catch { }
            return Task.CompletedTask;
        }

        protected virtual void HandleRequestV1(HttpListenerContext context)
        {
            string url = context.Request.RawUrl.Replace("/v1/", "");
            Logger.Verbose($"Handling Request: {context.Request.HttpMethod} @ {context.Request.RawUrl}");
            if (url.StartsWith("pushevent"))
                HandlePushEventV1(context, url.Replace("pushevent", ""));
            else if (url.StartsWith("ping-reply"))
            {
                var msg = GetMessage(context);
                Logger.Debug($"CommBus WASM Version: {msg.data}");
                AppService.Instance.CommBus.IsPingReceived = true;
            }
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
                var msg = GetMessage(context);
                Logger.Verbose($"Received Push Event: {msg.@event}");
                if (msg.@event == GsxConstants.EventCommBus)
                    AppService.Instance.GsxController.Menu.OnToolbarEvent(msg.data);
                else
                    AppService.Instance.CommBus.PushEvent(msg.@event, msg.data);
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual MessageReceive GetMessage(HttpListenerContext context)
        {
            using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
            return MessageReceive.Parse(reader.ReadToEnd());
        }
    }
}
