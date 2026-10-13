using Any2GSX.AppConfig;
using Any2GSX.Notifications;
using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.PluginInterface.Interfaces.GSX;
using Any2GSX.PluginInterface.Interfaces.GSX.API;
using Any2GSX.PluginInterface.Interfaces.GSX.API.Messages;
using Any2GSX.PluginInterface.Interfaces.GSX.API.Payload;
using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Any2GSX.GSX.API
{
    public class GsxApiClient() : IDisposable, IGsxApiClient
    {
        private bool _isDisposed;
        protected virtual CancellationTokenSource LinkedTokenSource { get; set; }
        protected virtual CancellationToken RequestToken => LinkedTokenSource?.Token ?? default;
        protected virtual Task RunTask { get; set; }
        protected virtual ClientWebSocket WebSocket { get; set; }
        public virtual bool IsSocketValid => WebSocket != null && WebSocket.State == WebSocketState.Open && !RequestToken.IsCancellationRequested;
        protected virtual ulong ByteCounter { get; set; } = 0;
        protected virtual DispatcherTimerAsync DispatcherTimer { get; } = new();

        protected virtual AppService AppService => Any2GSX.Instance.AppService;
        protected virtual Config Config => AppService.Config;
        public virtual GsxApiData ApiData { get; protected set; } = new();
        public virtual IGsxApiData IApiData => ApiData;
        public virtual bool IsSubscribed { get; protected set; } = false;
        public virtual bool HelloReceived { get; protected set; } = false;
        public virtual bool IsConnected => IsSocketValid && HelloReceived && IsSubscribed;
        public virtual bool IsReady => IsConnected && ApiData?.CheckApiState() == true;
        protected virtual bool LastReady { get; set; } = false;
        protected virtual string LastAirportSet { get; set; } = "";
        protected virtual string LastGateSet { get; set; } = "";
        protected virtual JsonNode LastAircraftHandler { get; set; } = JsonValue.Create(JsonValueKind.Null);
        protected virtual ConcurrentDictionary<string, GsxMessageResult> CommandResults { get; } = [];

        public event Func<GsxMenuState, string, string[], Task> MenuDataReceived;
        public event Func<bool, Task> ReadyStateChanged;
        public event Func<string, Task> AirportNameChanged;
        public event Func<string, Task> GateNameChanged;
        public event Func<Task> AircraftHandlerChanged;

        public virtual void Start()
        {
            if (RunTask != null && !RunTask.IsCompleted)
            {
                Logger.Debug("GSX API Task already running");
                return;
            }

            Logger.Debug("Starting GSX API Task ...");
            LinkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(AppService.RequestToken);
            RunTask = Task.Factory.StartNew(async () => await Run(), AppService.Token, TaskCreationOptions.AttachedToParent | TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public virtual async Task Stop()
        {
            if (LinkedTokenSource == null || RunTask == null)
                return;

            DispatcherTimer?.Stop();
            DispatcherTimer?.Dispose();
            await DisconnectApi();

            Logger.Debug("Stopping GSX API Task ...");
            LinkedTokenSource.Cancel();
            try
            {
                await RunTask;
            }
            catch (OperationCanceledException) { }
            finally
            {
                Cleanup();
                ResetState();
            }
        }

        protected virtual void ResetState()
        {
            ApiData = new();
            IsSubscribed = false;
            HelloReceived = false;
            LastReady = false;
            LastAirportSet = "";
            LastGateSet = "";
            NotifyHandlerData();
            CommandResults.Clear();
        }

        protected virtual void Cleanup()
        {
            ByteCounter = 0;
            LinkedTokenSource?.Dispose();
            LinkedTokenSource = null;
            RunTask = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                LinkedTokenSource?.Dispose();
                WebSocket?.Dispose();
            }
            _isDisposed = true;
        }

        protected virtual void LogException(Exception ex)
        {
            if (!AppService.Instance.NotificationTracker.IsActive(AppNotification.GsxRestart))
                Logger.Warning($"{ex.GetType().Name} on GSX API Connection: {ex.Message}");
            else
                Logger.Debug($"{ex.GetType().Name} on GSX API Connection: {ex.Message}");
        }

        public virtual async Task Disconnect()
        {
            HelloReceived = false;
            await DisconnectApi();
        }

        protected virtual async Task DisconnectApi()
        {
            Logger.Debug($"Disconnect from GSX API (total Traffic {ByteCounter / 1024} KiB) ...");
            if (IsSocketValid)
            {
                try
                {
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1000));
                    await SendUnsubscribe(timeoutCts.Token);
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }

                try
                {
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1000));
                    await WebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Any2GSX closing", timeoutCts.Token);
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            }
        }

        protected virtual async Task Run()
        {
            Logger.Debug("GSX API Task started");
            DispatcherTimer.Interval = TimeSpan.FromSeconds(Config.GsxApiRetryInterval);
            DispatcherTimer.Tick += CheckConnection;
            DispatcherTimer.Start();

            while (!RequestToken.IsCancellationRequested)
            {
                try
                {
                    ResetState();
                    WebSocket = new ClientWebSocket();
                    WebSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(Config.GsxApiKeepAlive);
                    WebSocket.Options.KeepAliveTimeout = TimeSpan.FromMilliseconds(Config.HttpRequestTimeoutMs);
                    await WebSocket.ConnectAsync(new(string.Format(Config.GsxApiConnectString, GsxConstants.GetRemotePort())), RequestToken);
                    Logger.Debug($"GSX Web Socket connected. Waiting for hello ...");

                    await ProcessReceived();
                }
                catch (OperationCanceledException) when (RequestToken.IsCancellationRequested)
                {
                    Logger.Debug($"Cancellation received for GSX API Task");
                    break;
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
                finally
                {
                    NotifyState();
                    WebSocket?.Dispose();
                    WebSocket = null;
                }

                if (!RequestToken.IsCancellationRequested)
                {
                    Logger.Debug($"GSX Web Socket not connected, Retry in {Config.GsxApiRetryInterval / 1000.0}s");
                    try
                    {
                        await Task.Delay(Config.GsxApiRetryInterval, RequestToken);
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.Debug($"Cancellation received for GSX API Task");
                        break;
                    }
                }
            }

            Logger.Debug("GSX API Task stopped.");
        }

        protected virtual Task CheckConnection()
        {
            if (RequestToken.IsCancellationRequested)
            {
                DispatcherTimer?.Stop();
                return Task.CompletedTask;
            }

            if (!IsReady && IsConnected)
            {
                Logger.Debug($"Stale GSX API Data - requesting State ...");
                return GetState();
            }

            return Task.CompletedTask;
        }

        protected virtual async Task ProcessReceived()
        {
            if (!IsSocketValid)
                return;

            var buffer = new byte[Config.WsReceiveBuffer];
            while (IsSocketValid)
            {
                var segment = new ArraySegment<byte>(buffer);
                WebSocketReceiveResult result;

                using var ms = new MemoryStream();
                do
                {
                    result = await WebSocket.ReceiveAsync(segment, RequestToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Logger.Information($"Close Request received from GSX API");
                        return;
                    }

                    ms.Write(buffer, 0, result.Count);
                    ByteCounter += (ulong)result.Count;
                }
                while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    string jsonString;
                    using (var reader = new StreamReader(ms, Encoding.UTF8))
                    {
                        jsonString = await reader.ReadToEndAsync();
                    }

                    OnMessageReceived(jsonString);
                }
            }
        }

        protected virtual void LogMessage(GsxMessageEnvelop message, string text, LogLevel level = LogLevel.Debug)
        {
            Logger.Log(level, $"{text}: {message}");
        }

        protected virtual void OnMessageReceived(string jsonString)
        {
            try
            {
                var message = GsxMessageEnvelop.Parse(jsonString);

                if (message.v == 1)
                {
                    switch (message.MessageType)
                    {
                        case GsxMessageType.hello:
                            ParseMessageHello(message);
                            break;
                        case GsxMessageType.snapshot:
                            ParseMessageSnapshot(message);
                            break;
                        case GsxMessageType.patch:
                            ParseMessagePatch(message);
                            break;
                        case GsxMessageType.result:
                            ParseMessageResult(message);
                            break;
                        case GsxMessageType.@event:
                            ParseMessageEvent(message);
                            break;
                        default:
                            LogMessage(message, "Unknown GSX API Message", LogLevel.Debug);
                            break;
                    }

                    NotifyState();
                }
                else
                {
                    Logger.Error($"Unsupported GSX API Version!");
                    Logger.Debug(message.ToString());
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        protected virtual void NotifyState()
        {
            bool isReady = IsReady;

            if (LastReady != isReady)
            {
                Logger.Debug($"GSX API Client Ready State changed {LastReady} -> {isReady}");
                if (!RequestToken.IsCancellationRequested)
                    _ = TaskTools.RunPool(() => ReadyStateChanged?.Invoke(isReady), RequestToken);
            }
            LastReady = isReady;
        }

        protected virtual void ParseMessageHello(GsxMessageEnvelop message)
        {
            var hello = message.ParseHello();

            if (hello.IsValid)
            {
                ApiData.ParseDataHello(hello);
                if (!HelloReceived)
                {
                    Logger.Debug($"GSX API 'hello' received. Subscribe Channels ...");
                    HelloReceived = true;
                    _ = TaskTools.RunPool(SendSubscribe, RequestToken);
                }
            }
            else
                LogMessage(message, "Invalid hello Message", LogLevel.Debug);
        }

        protected virtual void ParseMessageSnapshot(GsxMessageEnvelop message)
        {
            if (message.HasKey("state", JsonValueKind.Number))
            {
                if (message.GetKey(GsxPayloadName.handlerData, out var handlerData))
                    ParseDataHandler(handlerData);
                if (message.GetKey(GsxPayloadName.startup, out var startup))
                    ApiData.ParseDataStartup(startup);
                if (message.GetKey(GsxPayloadName.services, out var services))
                    ApiData.ParseDataServices(services);
                if (message.GetKey(GsxPayloadName.menuShown, out var menuShown))
                    ParseDataMenuShown(menuShown);
                if (message.GetKey(GsxPayloadName.menu, out var menu))
                    ParseDataMenu(menu);
                if (message.GetKey(GsxPayloadName.search, out var search))
                    ApiData.ParseDataSearch(search);
                if (message.GetKey(GsxPayloadName.state, out var state))
                    ApiData.ParseDataState(state);
                if (message.GetKey(GsxPayloadName.settings, out var settings))
                    ApiData.ParseDataSettings(settings);
            }
            else
                LogMessage(message, "Invalid snapshot Message", LogLevel.Debug);
        }

        protected virtual void ParseMessagePatch(GsxMessageEnvelop message)
        {
            if (message.HasKey("path") && message.HasKey("value"))
            {
                var patch = message.ParsePatch();
                var path = patch.PayloadPath;
                switch (path)
                {
                    case GsxPayloadName.handlerData:
                        ParseDataHandler(patch.value);
                        break;
                    case GsxPayloadName.startup:
                        ApiData.ParseDataStartup(patch.value);
                        break;
                    case GsxPayloadName.services:
                        ApiData.ParseDataServices(patch.value);
                        break;
                    case GsxPayloadName.menuShown:
                        ParseDataMenuShown(patch.value);
                        break;
                    case GsxPayloadName.menu:
                        ParseDataMenu(patch.value);
                        break;
                    case GsxPayloadName.search:
                        ApiData.ParseDataSearch(patch.value);
                        break;
                    case GsxPayloadName.state:
                        ApiData.ParseDataState(patch.value);
                        break;
                    case GsxPayloadName.settings:
                        ApiData.ParseDataSettings(patch.value);
                        break;
                    default:
                        if (Config.VerboseLogging)
                            LogMessage(message, "Unhandled Path in patch Message", LogLevel.Warning);
                        break;
                }
            }
            if (!message.HasKey("path"))
                LogMessage(message, "Invalid patch Message", LogLevel.Warning);
        }

        protected virtual void ParseMessageResult(GsxMessageEnvelop message)
        {
            GsxMessageResult result = message.ParseResult();

            if (!IsSubscribed && HelloReceived && result.id == "subscribe")
            {
                IsSubscribed = true;
                Logger.Debug($"GSX API subscribed. Waiting for State ...");
            }
            else if (result.HasId)
                CommandResults.TryAdd(result.id, result);
            else
            {
                LogMessage(message, "Empty result Message", LogLevel.Debug);
                return;
            }

            if (!result.ok)
            {
                Logger.Warning($"GSX API Result '{result.error.code}': {result.error.message}");
            }
        }

        protected virtual void ParseMessageEvent(GsxMessageEnvelop message)
        {
            if (message.HasKey("topic"))
            {
                var @event = message.ParseEvent();
                switch (@event.topic)
                {
                    case "engine":
                        ApiData.ParseDataEvent(@event);
                        break;
                    case "startup":
                        ApiData.ParseDataEvent(@event);
                        break;
                    default:
                        LogMessage(message, "Unknown Topic in Event Message", LogLevel.Warning);
                        break;
                }
            }
            else
                LogMessage(message, "Invalid patch Message", LogLevel.Warning);
        }

        protected virtual void ParseDataHandler(JsonNode node)
        {
            ApiData.ParseDataHandler(node);

            AppService.Instance.NotificationTracker.CaptureActiveGate(ApiData?.HandlerData?.gate?.uiGateName ?? "");
            NotifyHandlerData();

            if (node!?["aircraft"]?.ToJsonString() != LastAircraftHandler?.ToJsonString())
            {
                Logger.Debug($"GSX Aircraft Handler has changed (not null: {ApiData?.HandlerData?.aircraft != null})");
                _ = TaskTools.RunPool(() => AircraftHandlerChanged?.Invoke());
            }
            LastAircraftHandler = node!?["aircraft"];
        }

        protected virtual void NotifyHandlerData()
        {
            if (LastAirportSet != (ApiData?.HandlerData?.airport?.icao ?? ""))
            {
                LastAirportSet = ApiData?.HandlerData?.airport?.icao ?? "";
                if (!string.IsNullOrWhiteSpace(LastAirportSet))
                    Logger.Debug($"GSX API Airport changed to {LastAirportSet}");
                else
                    Logger.Debug($"GSX API Airport cleared");

                _ = TaskTools.RunPool(() => AirportNameChanged?.Invoke(LastAirportSet), RequestToken);
            }

            if (LastGateSet != (ApiData?.HandlerData?.gate?.uiName ?? ""))
            {
                LastGateSet = ApiData?.HandlerData?.gate?.uiName ?? "";
                if (!string.IsNullOrWhiteSpace(LastGateSet))
                    Logger.Debug($"GSX API Gate changed to {LastGateSet}");
                else
                    Logger.Debug($"GSX API Gate cleared");

                _ = TaskTools.RunPool(() => GateNameChanged?.Invoke(LastGateSet), RequestToken);
            }
        }

        public virtual void ParseDataMenuShown(JsonNode node)
        {
            ApiData.ParseDataMenuShown(node);
            if (ApiData?.MenuShown == false)
                _ = TaskTools.RunPool(() => MenuDataReceived?.Invoke(AppService.Instance?.GsxController?.Menu?.FirstReadyReceived == false ? GsxMenuState.READY : GsxMenuState.TIMEOUT, "", new string[10]), RequestToken);
        }

        public virtual void ParseDataMenu(JsonNode menu)
        {
            ApiData.ParseDataMenu(menu);
            if (!string.IsNullOrWhiteSpace(ApiData?.Menu?.title) && ApiData?.Menu?.entries?.Count > 0)
                _ = TaskTools.RunPool(() => MenuDataReceived?.Invoke(GsxMenuState.READY, ApiData?.Menu?.title, [.. ApiData?.Menu?.entries]), RequestToken);
        }

        protected virtual async Task<bool> SendJsonRequest<T>(T @object) where T : class
        {
            try
            {
                if (IsSocketValid)
                {
                    await WebSocket.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize<T>(@object)), WebSocketMessageType.Text, true, RequestToken);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }

            return false;
        }

        protected virtual async Task<bool> ApiCommandWaiter(GsxApiCommand apiCommand)
        {
            int timeWaited = 0;
            int wait = 50;
            int timeout = Config.GsxApiResultTimeout;

            try
            {
                GsxMessageResult result = null;
                do
                {
                    await Task.Delay(wait);
                    timeWaited += wait;
                }
                while (timeWaited <= timeout && !CommandResults.TryGetValue(apiCommand.id, out result) && !RequestToken.IsCancellationRequested);

                if (timeWaited > timeout)
                    apiCommand.SetResult(false, "wait_timed_out");
                else if (result != null)
                {
                    apiCommand.SetResult(result);
                    CommandResults.TryRemove(apiCommand.id, out _);
                }
                else
                    apiCommand.SetResult(false, "wait_null_result");

            }
            catch (Exception ex)
            {
                LogException(ex);
            }

            return apiCommand.HasError;
        }

        protected virtual async Task<GsxMessageResult> SendApiCommand(GsxApiCommand apiCommand)
        {
            try
            {
                if (IsSocketValid)
                {
                    Logger.Debug($"GSX API: Command '{apiCommand.id}': {JsonSerializer.Serialize(apiCommand.args)}");
                    await WebSocket.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(apiCommand)), WebSocketMessageType.Text, true, RequestToken);
                    await ApiCommandWaiter(apiCommand);
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }

            return apiCommand.result;
        }

        protected virtual async Task<bool> SendJsonNode(string json, CancellationToken? token = null)
        {
            try
            {
                if (IsSocketValid)
                {
                    await WebSocket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, token ?? RequestToken);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }

            return false;
        }

        protected virtual Task<bool> SendSubscribe()
        {
            return SendJsonNode(JsonSerializer.Serialize(GsxApiMsgSubscribe.Subscribe()));
        }

        protected virtual Task<bool> SendUnsubscribe(CancellationToken? token = null)
        {
            IsSubscribed = false;

            token ??= AppService.Instance.Token;
            if (token?.IsCancellationRequested == false)
                try { return SendJsonNode(JsonSerializer.Serialize(GsxApiMsgSubscribe.Unsubscribe()), token); } catch { }

            return Task.FromResult(false);
        }

        protected virtual Task<bool> SendDictionary(Dictionary<string, object> message)
        {
            return SendJsonNode(JsonSerializer.Serialize(message));
        }

        public virtual Task<GsxMessageResult> GetState()
        {
            return SendApiCommand(GsxApiCommand.Create<GsxApiCmdGetState>(new object()));
        }

        public virtual Task<GsxMessageResult> CallService(GsxServiceType type)
        {
            string id = GsxPayloadService.ReverseLookup(type);
            return SendApiCommand(GsxApiCommand.Create<GsxApiCmdCallService>(new Dictionary<string, object> { { "service", id } }));
        }

        public virtual Task<GsxMessageResult> MenuOpen()
        {
            return SendApiCommand(GsxApiCommand.Create<GsxApiCmdMenuOpen>(new object()));
        }

        public virtual Task<GsxMessageResult> MenuClose()
        {
            return SendApiCommand(GsxApiCommand.Create<GsxApiCmdMenuClose>(new object()));
        }

        public virtual Task<GsxMessageResult> MenuSelect(int index)
        {
            return SendApiCommand(GsxApiCommand.Create<GsxApiCmdMenuSelect>(new Dictionary<string, object> { { "index", index } }));
        }

        public virtual Task<GsxMessageResult> MenuSearch(string text)
        {
            return SendApiCommand(GsxApiCommand.Create<GsxApiCmdMenuSearch>(new Dictionary<string, object> { { "text", text } }));
        }

        public virtual dynamic GetSetting(string key)
        {
            try
            {
                if (ApiData?.SettingMap?.TryGetValue(key, out var setting) == true)
                    return setting.GetValue();
                else
                {
                    Logger.Debug($"Setting '{key}' does not exist in SettingMap");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return 0;
            }
        }

        public virtual T GetSetting<T>(string key)
        {
            try
            {
                if (ApiData?.SettingMap?.TryGetValue(key, out var setting) == true)
                    return (T)setting.GetValue();
                else
                {
                    Logger.Debug($"Setting '{key}' does not exist in SettingMap");
                    return default;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return default;
            }
        }

        public virtual Task<GsxMessageResult> SetSetting(string key, dynamic value)
        {
            return SendApiCommand(GsxApiCommand.Create<GsxApiCmdSetSetting>(new Dictionary<string, object> { { "key", key }, { "value", value } }));
        }

        public virtual Task<GsxMessageResult> SetHandlerGate(string prop, dynamic value)
        {
            return SendApiCommand(GsxApiCommand.Create<GsxApiCmdSetHandler>(new Dictionary<string, object> { { "target", "gate" }, { "name", prop }, { "value", value } }));
        }

        public virtual Task<GsxMessageResult> SetHandlerAircraft(string prop, dynamic value)
        {
            return SendApiCommand(GsxApiCommand.Create<GsxApiCmdSetHandler>(new Dictionary<string, object> { { "target", "aircraft" }, { "name", prop }, { "value", value } }));
        }

        public virtual Task<GsxMessageResult> GateSelect(string name, bool force = true)
        {
            return SendApiCommand(GsxApiCommand.Create<GsxApiCmdSelectGate>(new Dictionary<string, object> { { "gate", name }, { "force", force } }));
        }

        public virtual Task<GsxMessageResult> GateSelect(int number, bool force = true)
        {
            return SendApiCommand(GsxApiCommand.Create<GsxApiCmdSelectGate>(new Dictionary<string, object> { { "gate", number }, { "force", force } }));
        }

        public virtual Task<GsxMessageResult> InputSubmit(int session, string text)
        {
            return SendApiCommand(GsxApiCommand.Create<GsxApiCmdInputSubmit>(new Dictionary<string, object> { { "gen", session }, { "text", text } }));
        }

        public virtual Task<GsxMessageResult> InputCancel(int session)
        {
            return SendApiCommand(GsxApiCommand.Create<GsxApiCmdInputCancel>(new Dictionary<string, object> { { "gen", session } }));
        }

        public virtual Task<GsxMessageResult> CommandRun(GsxCommandRunType type)
        {
            return SendApiCommand(GsxApiCommand.Create<GsxApiCmdRunCommand>(new Dictionary<string, object> { { "command", IGsxApiClient.CommandRunTypes[type] } }));
        }
    }
}
