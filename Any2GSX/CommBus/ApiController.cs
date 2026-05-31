using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.GSX;
using Any2GSX.GSX.Automation;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppFramework.Services;
using CFIT.AppLogger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;

namespace Any2GSX.CommBus
{
    public class ApiController(Config config) : ServiceController<Any2GSX, AppService, Config, Definition>(config)
    {
        public virtual HttpListener HttpListener { get; } = new();
        protected virtual GsxAutomationController AutomationController => AppService.Instance.GsxController.AutomationController;
        protected virtual Flightplan Flightplan => AppService.Instance.Flightplan;
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

        protected virtual async Task SendJson(string json, HttpListenerContext context)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            context.Response.ContentType = "application/json";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = buffer.Length;
            context.Response.StatusCode = (int)HttpStatusCode.OK;

            using var output = context.Response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
        }

        protected virtual void SetOk(HttpListenerContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;
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
                        await HandleRequestV1(context);
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

        protected virtual async Task HandleRequestV1(HttpListenerContext context)
        {
            string url = context.Request.RawUrl.Replace("/v1/", "");
            Logger.Verbose($"Handling Request: {context.Request.HttpMethod} @ {context.Request.RawUrl}");
            if (url.StartsWith("pushevent"))
                HandlePushEventV1(context, url.Replace("pushevent", ""));
            else if (url.StartsWith("external"))
                HandleExternalRequestEventV1(context, url.Replace("external", ""));
            else if (url.StartsWith("ping-reply"))
            {
                var msg = GetMessage(context);
                Logger.Debug($"CommBus WASM Version: {msg.data}");
                AppService.Instance.CommBus.IsPingReceived = true;
                AppService.Instance.UpdatePortFile();
            }
            else if (url.StartsWith("status"))
            {
                var response = new Dictionary<string, object>()
                {
                    { "state", AppService.Instance.IsSessionInitialized ? "Session" : "Running" },
                    { "phase", AppService.Instance.GsxController.AutomationState.ToString() },
                    { "profile", AppService.Instance.ProfileName },
                    { "externalControl", AppService.Instance.GsxController.AutomationController.ExternalServiceControl }
                };
                await SendJson(JsonSerializer.Serialize(response), context);
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

        /// {
        ///     "request": "setExternal" | "callService" | "setPaxCount" | "setPaxDiff" | "setFuelPlanned" | "sendFinal"
        ///     "data": true|false | { "service": string (GsxServiceType Name), "activation": string (GsxServiceActivation Name) } | int | int | int | empty
        /// }
        protected virtual void HandleExternalRequestEventV1(HttpListenerContext context, string url)
        {
            try
            {
                var node = GetNode(context);
                var request = node!["request"]!?.ToString();

                if (string.IsNullOrEmpty(request))
                {
                    Logger.Error("Received invalid External Request");
                    return;
                }
                else
                    Logger.Debug($"Received External Request: {request}");

                if (request.Equals("setExternal", StringComparison.InvariantCultureIgnoreCase) && (node!["data"]!?.GetValueKind() == JsonValueKind.True || node!["data"]!?.GetValueKind() == JsonValueKind.False))
                {
                    AutomationController.DepartureQueue.SetExternalControl(node["data"].GetValue<bool>());
                    SetOk(context);
                }
                else if (request.Equals("callService", StringComparison.InvariantCultureIgnoreCase) && node!["data"]!?.GetValueKind() == JsonValueKind.Object)
                {
                    if (Enum.TryParse<GsxServiceType>(node!["data"]!["service"]!?.ToString(), out var serviceType))
                    {
                        if (!Enum.TryParse<GsxServiceActivation>(node!["data"]!["activation"]!?.ToString(), out var serviceActivation))
                            serviceActivation = GsxServiceActivation.Manual;
                        Logger.Information($"External Service Request for {serviceType} (Activation: {serviceActivation})");
                        if (AutomationController.DepartureQueue.QueueExternalService(serviceType, serviceActivation))
                            SetOk(context);
                        else
                            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                }
                else if (request.Equals("setPaxCount", StringComparison.InvariantCultureIgnoreCase) && node!["data"]!?.GetValueKind() == JsonValueKind.Number)
                {
                    int planned = node!["data"]!?.GetValue<int>() ?? Flightplan.CountPaxPlanned;
                    Logger.Information($"Planned Pax updated externally to {planned}");
                    Flightplan.UpdatePassengerCount(planned, Flightplan.DiffPax);
                    Flightplan.UpdateBagCount(planned, Flightplan.DiffPax);
                    SetOk(context);
                }
                else if (request.Equals("setPaxDiff", StringComparison.InvariantCultureIgnoreCase) && node!["data"]!?.GetValueKind() == JsonValueKind.Number)
                {
                    int diff = node!["data"]!?.GetValue<int>() ?? Flightplan.DiffPax;
                    Logger.Information($"Pax Difference updated externally to {diff}");
                    Flightplan.UpdatePassengerCount(Flightplan.CountPaxPlanned, diff);
                    Flightplan.UpdateBagCount(Flightplan.CountPaxPlanned, diff);
                    SetOk(context);
                }
                else if (request.Equals("setFuelPlanned", StringComparison.InvariantCultureIgnoreCase) && node!["data"]!?.GetValueKind() == JsonValueKind.Number)
                {
                    int fuel = node!["data"]!?.GetValue<int>() ?? (int)Flightplan.FuelRampKg;
                    Logger.Information($"Planned Fuel updated externally to {fuel}");
                    Flightplan.UpdatePlannedFuelKg(fuel);
                    SetOk(context);
                }
                else if (request.Equals("sendFinal", StringComparison.InvariantCultureIgnoreCase))
                {
                    Logger.Information($"External Loadsheet received");
                    AutomationController.SendExternalLoadsheet();
                    SetOk(context);
                }
                else
                {
                    Logger.Error($"Could not match Request or Data");
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }

        protected virtual MessageReceive GetMessage(HttpListenerContext context)
        {
            using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
            return MessageReceive.Parse(reader.ReadToEnd());
        }

        protected virtual JsonNode GetNode(HttpListenerContext context)
        {
            try
            {
                using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                return JsonNode.Parse(reader.ReadToEnd());
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return null;
            }
        }
    }
}
