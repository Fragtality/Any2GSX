using Any2GSX.AppConfig;
using CFIT.AppFramework.Services;
using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace Any2GSX.Tools
{
    public abstract class AppServer() : ServiceController<Any2GSX, AppService, Config, Definition>(AppService.Instance.Config)
    {
        public virtual HttpListener HttpListener { get; protected set; } = null;
        protected static int InstanceId { get; set; } = 0;
        protected static bool IsScanning { get; set; } = false;
        protected virtual AppService AppService => AppService.Instance;
        public virtual int Port { get; protected set; } = AppService.Instance.Config.PortMin + InstanceId++;
        public abstract List<string> SupportedVersions { get; }
        public virtual Dictionary<string, Dictionary<string, Func<RequestEnvelope, Task>>> SupportedEndpoints { get; } = [];
        public static JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        protected override Task DoInit()
        {
            foreach (var version in SupportedVersions)
                SupportedEndpoints.Add(version, []);
            RegisterEndpoints();

            return Task.CompletedTask;
        }

        protected abstract void RegisterEndpoints();

        protected virtual void TryClose()
        {
            try { HttpListener?.Stop(); } catch { }
            try { HttpListener?.Close(); } catch { }
        }

        protected override Task DoCleanup()
        {
            IsScanning = false;
            TryClose();
            return Task.CompletedTask;
        }

        public virtual string GetName()
        {
            return this.GetType().Name?.Replace("Server", " Server") ?? "App Server";
        }

        protected virtual void DumpData(RequestEnvelope request)
        {
            Logger.Debug($"Request Path: {request?.RequestPath} | Request Data: {request?.JsonData?.ToJsonString()}");
        }

        protected virtual void LogWarning(string text, RequestEnvelope request)
        {
            Logger.Warning($"Received Request {text} on {GetName()}");
            DumpData(request);
        }

        protected virtual int ScanPorts(int nextPort)
        {
            try
            {
                var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                var tcpConnections = ipGlobalProperties.GetActiveTcpConnections();
                var tcpListeners = ipGlobalProperties.GetActiveTcpListeners();

                var usedPorts = new HashSet<int>();
                foreach (var conn in tcpConnections) usedPorts.Add(conn.LocalEndPoint.Port);
                foreach (var listener in tcpListeners) usedPorts.Add(listener.Port);

                int portMin = nextPort > Config.PortMin ? nextPort : Config.PortMin;
                for (int port = portMin; port <= Config.PortMax; port++)
                {
                    if (!usedPorts.Contains(port))
                    {
                        try
                        {
                            var tcpListener = new TcpListener(IPAddress.Loopback, port);
                            tcpListener.Start();
                            tcpListener.Stop();
                            return port;
                        }
                        catch (SocketException) { }
                    }
                }

            }
            catch (Exception ex)
            {
                if (Config.LogLevel == LogLevel.Verbose)
                    Logger.LogException(ex);
                Logger.Debug($"{ex.GetType().Name} while finding Port for {GetName()}");
            }

            return 0;
        }

        protected virtual bool StartListener()
        {
            try
            {
                HttpListener = new();
                HttpListener.Prefixes.Add($"http://localhost:{Port}/");
                HttpListener.Start();
                return true;
            }
            catch (Exception ex)
            {
                if (Config.LogLevel == LogLevel.Verbose)
                    Logger.LogException(ex);
                Logger.Debug($"{ex.GetType().Name} while starting HttpListener for {GetName()} on Port {Port}");
                HttpListener = null;
            }

            return false;
        }

        protected override async Task DoRun()
        {
            await Task.Delay(Random.Shared.Next(50, 100), Token);
            while (IsScanning && !Token.IsCancellationRequested)
                await Task.Delay(250, Token);
            IsScanning = true;

            for (int i = 0; i < Config.PortFindRetries; i++)
            {
                Port = ScanPorts(Port);
                if (Port <= 0)
                {
                    await Task.Delay(Config.PortFindRetryInterval, Token);
                    continue;
                }

                if (StartListener())
                    break;
                else if (i + 1 < Config.PortFindRetries)
                {
                    Port++;
                    await Task.Delay(Config.PortFindRetryInterval, Token);
                }
            }

            if (HttpListener == null || HttpListener?.IsListening == false)
            {
                Logger.Error($"HttpListener for {GetName()} could not be started!");
                MessageBox.Show($"The HTTP Listener for {GetName()} could not be started but is required to run the App.\r\nAny2GSX will exit now. If the Problem continues, try a Reboot of your PC.", $"HTTP Listener could not be started!", MessageBoxButton.OK, MessageBoxImage.Error);
                Any2GSX.Instance.RequestShutdown(-1);
                throw new Exception($"HttpListener for {GetName()} could not be started!");
            }
            IsScanning = false;

            await AfterStart();
            Logger.Information($"{GetName()} started on Port {Port}");

            while (IsExecutionAllowed && !Token.IsCancellationRequested)
            {
                try
                {
                    var context = await HttpListener.GetContextAsync().WaitAsync(Token);
                    if (Config.VerboseLogging)
                        Logger.Verbose($"Handling Request: {context?.Request?.HttpMethod ?? ""} @ {context?.Request?.RawUrl ?? ""}");
                    _ = TaskTools.RunPool(() => ProcessRequest(new RequestEnvelope(context)));
                }
                catch (Exception ex)
                {
                    if (ex is not OperationCanceledException && ex is not TaskCanceledException)
                    {
                        Logger.Error($"{ex.GetType().Name} while receiving Requests in {GetName()}");
                        Logger.LogException(ex);
                    }
                }
            }

            TryClose();
            Logger.Information($"{GetName()} ended");
        }

        protected virtual Task AfterStart()
        {
            return Task.CompletedTask;
        }

        protected virtual async Task<RequestEnvelope> ReceiveMessage()
        {
            var context = await HttpListener.GetContextAsync().WaitAsync(Token);
            if (Config.VerboseLogging)
                Logger.Verbose($"Handling Request: {context?.Request?.HttpMethod ?? ""} @ {context?.Request?.RawUrl ?? ""}");
            return new RequestEnvelope(context);
        }

        protected virtual async Task ProcessRequest(RequestEnvelope request)
        {
            if (SupportedVersions.Any(v => v.Equals(request.Version)))
            {
                try
                {
                    request.ParseData();
                    var query = SupportedEndpoints!?[request.Version]!?.Where(kv => kv.Key.Equals(request.Endpoint, StringComparison.InvariantCultureIgnoreCase));
                    if (query?.Any() == true)
                    {
                        var func = query.FirstOrDefault().Value;
                        if (func != null)
                            await func?.Invoke(request);
                        else
                        {
                            Logger.Error($"No Function found to handle Endpoint '{request.Endpoint}' (Version {request.Version})");
                            DumpData(request);
                            request.SetResponseCode(HttpStatusCode.InternalServerError, "Function not found");
                        }
                    }
                    else
                    {
                        LogWarning($"for unsupported Endpoint '{request.Endpoint}' ({request.Version})", request);
                        request.SetResponseCode(HttpStatusCode.BadRequest, "Unsupported Endpoint");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"{ex.GetType().Name} while processing Request in {GetName()}");
                    DumpData(request);
                    if (ex is JsonException)
                        request.SetResponseCode(HttpStatusCode.BadRequest, "JSON Data could not be parsed");
                    else
                        request.SetResponseCode(HttpStatusCode.InternalServerError, $"The Request threw a {ex.GetType().Name}");
                }
            }
            else
            {
                LogWarning($"with unsupported Version '{request.Version}'", request);
                request.SetResponseCode(HttpStatusCode.BadRequest, "Unsupported API Version");
            }

            if (!request.Handled)
                request.SetResponseCode(HttpStatusCode.InternalServerError, "The Request was not handled");

            request.Close();
        }

        protected virtual T DeserializeRequest<T>(RequestEnvelope request)
        {
            return JsonSerializer.Deserialize<T>(request.JsonData, SerializerOptions);
        }
    }
}
