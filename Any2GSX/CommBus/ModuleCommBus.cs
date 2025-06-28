using Any2GSX.Notifications;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
using CFIT.SimConnectLib.Modules;
using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Any2GSX.CommBus
{
    public class ModuleCommBus(SimConnectManager manager, object moduleParams) : SimConnectModule(manager, moduleParams), ICommBus
    {
        protected const string CDA_NAME_REQUEST = "Any2GSX_CommBus.Request";
        public const int AREA_SIZE = 8192;

        protected virtual bool ClientDataAreaCreated { get; set; } = false;
        protected virtual ConcurrentDictionary<string, Action<string, string>> EventCallbacks { get; } = [];
        public virtual bool IsPingReceived { get; set; } = false;
        public virtual bool IsConnected => IsPingReceived && IsReceiveRunning;
        public virtual int Port { get; set; } = 0;
        protected virtual DateTime NextPing { get; set; } = DateTime.MinValue;

        protected override void SetModuleParams(object moduleParams)
        {

        }

        public override Task<int> CheckResources()
        {
            return Task.FromResult(0);
        }

        public override async Task CheckState()
        {
            if (!IsPingReceived && ClientDataAreaCreated && Manager.IsSessionRunning && Port > 0 && NextPing < DateTime.Now)
            {
                Logger.Debug($"Sending Ping to CommBus Module (Port {Port})");
                await PingModule(Port.ToString());
                NextPing = DateTime.Now + TimeSpan.FromMilliseconds(2000);
            }
        }

        public override Task ClearUnusedResources(bool clearAll)
        {
            return Task.CompletedTask;
        }

        public virtual async Task ResetSmartButton()
        {
            await ExecuteCalculatorCode($"0 (>{GenericSettings.VarSmartButtonDefault},number)");
        }

        public virtual async Task Reset()
        {
            await ResetSmartButton();
            await UnregisterAll();
            IsPingReceived = false;
        }

        public override async Task OnOpen(SIMCONNECT_RECV_OPEN evtData)
        {
            await base.OnOpen(evtData);
            await CreateDataArea();
        }

        public override void RegisterModule()
        {
            Manager.OnOpen += OnOpen;
        }

        public override async Task UnregisterModule(bool disconnect)
        {
            if (disconnect && Manager.IsReceiveRunning)
            {
                await UnregisterAll();
            }
        }

        protected virtual async Task CreateDataArea()
        {
            if (ClientDataAreaCreated)
                return;

            await Call(sc => sc.MapClientDataNameToID(CDA_NAME_REQUEST, ClientDataId.REQUEST));
            await Call(sc => sc.AddToClientDataDefinition(ClientDataId.REQUEST, 0, AREA_SIZE, 0, 0));

            ClientDataAreaCreated = true;
        }

        public virtual void PushEvent(string @event, string data)
        {
            if (EventCallbacks.TryGetValue(@event, out var callback))
                TaskTools.RunLogged(() => callback.Invoke(@event, data));
            else
                Logger.Warning($"No Callback found for Event '{@event}'");
        }

        public virtual async Task SendCommBus(string @event, string data, BroadcastFlag flag = BroadcastFlag.DEFAULT)
        {
            if (!Manager.IsReceiveRunning)
                return;

            try
            {
                var request = MessageRequest.Create(RequestType.CALL, @event, data, flag);
                var msg = new ModuleMessage()
                {
                    Data = request.Serialize()
                };

                await Call(sc => sc.SetClientData(ClientDataId.REQUEST,
                    ClientDataId.REQUEST,
                    SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT,
                    0,
                    msg
                    ));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public virtual async Task RegisterCommBus(string @event, BroadcastFlag eventSource, Action<string, string> callback)
        {
            if (!Manager.IsReceiveRunning)
                return;

            if (EventCallbacks.ContainsKey(@event))
            {
                Logger.Debug($"Event '{@event}' is already registered");
                return;
            }

            try
            {
                var request = MessageRequest.Create(RequestType.REGISTER, @event, "", eventSource);
                var msg = new ModuleMessage()
                {
                    Data = request.Serialize()
                };

                await Call(sc => sc.SetClientData(ClientDataId.REQUEST,
                    ClientDataId.REQUEST,
                    SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT,
                    0,
                    msg
                    ));

                EventCallbacks.Add(@event, callback);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public virtual async Task UnregisterCommBus(string @event, BroadcastFlag eventSource, Action<string, string> callback)
        {
            if (!Manager.IsReceiveRunning)
                return;

            if (!EventCallbacks.ContainsKey(@event))
            {
                Logger.Debug($"Event '{@event}' is not registered");
                return;
            }

            try
            {
                var request = MessageRequest.Create(RequestType.UNREGISTER, @event, "", eventSource);
                var msg = new ModuleMessage()
                {
                    Data = request.Serialize()
                };

                await Call(sc => sc.SetClientData(ClientDataId.REQUEST,
                    ClientDataId.REQUEST,
                    SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT,
                    0,
                    msg
                    ));

                EventCallbacks.Remove(@event);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public virtual async Task UnregisterAll()
        {
            try
            {
                if (Manager.QuitReceived)
                    return;

                var request = MessageRequest.Create(RequestType.REMOVEALL, "all");
                var msg = new ModuleMessage()
                {
                    Data = request.Serialize()
                };

                await Call(sc => sc.SetClientData(ClientDataId.REQUEST,
                    ClientDataId.REQUEST,
                    SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT,
                    0,
                    msg
                    ));

                EventCallbacks.Clear();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public virtual async Task PingModule(string portNumber)
        {
            try
            {
                var request = MessageRequest.Create(RequestType.PING, "all", portNumber);
                var msg = new ModuleMessage()
                {
                    Data = request.Serialize()
                };

                await Call(sc => sc.SetClientData(ClientDataId.REQUEST,
                    ClientDataId.REQUEST,
                    SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT,
                    0,
                    msg
                ));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public virtual async Task ExecuteCalculatorCode(string code)
        {
            try
            {
                Logger.Debug($"Sending CalculatorCode to the Module '{code}'");
                var request = MessageRequest.Create(RequestType.CODE, "code", code);
                var msg = new ModuleMessage()
                {
                    Data = request.Serialize()
                };

                await Call(sc => sc.SetClientData(ClientDataId.REQUEST,
                    ClientDataId.REQUEST,
                    SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT,
                    0,
                    msg
                ));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public virtual async Task SendEfbUpdate(EfbMessage message)
        {
            try
            {
                Logger.Verbose($"Sending Message to EFB App: {JsonSerializer.Serialize(message)}");
                var request = MessageRequest.Create(RequestType.EFB, "EfbUpdate", JsonSerializer.Serialize(message));
                var msg = new ModuleMessage()
                {
                    Data = request.Serialize()
                };

                await Call(sc => sc.SetClientData(ClientDataId.REQUEST,
                    ClientDataId.REQUEST,
                    SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT,
                    0,
                    msg
                ));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}
