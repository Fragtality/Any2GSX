using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppFramework.ResourceStores;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimEvents;
using CFIT.SimConnectLib.SimResources;
using CFIT.SimConnectLib.SimVars;
using Neo.IronLua;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Any2GSX.Aircraft
{
    public class AircraftLua(IAppResources appResources, string directory, string fileName) : AircraftBase(appResources)
    {
        public class LuaFunction(string name, dynamic func)
        {
            public virtual string Name { get; set; } = name;
            public virtual dynamic Function { get; set; } = func;
        }
        public virtual Lua LuaEngine { get; set; } = null;
        public override bool IsConnected => LuaEngine != null && InitComplete;
        protected virtual bool InitComplete { get; set; } = false;
        public virtual LuaGlobal LuaEnv { get; set; }
        public virtual LuaChunk LuaChunk { get; set; }
        public virtual string Directory { get; } = directory;
        public virtual string FileName { get; } = fileName;
        protected virtual ConcurrentDictionary<string, ISimResourceSubscription> ResourceSubscriptions { get; } = [];
        protected virtual List<CommBusSubscription> CommBusSubscriptions { get; } = [];
        protected virtual ConcurrentDictionary<GsxServiceType, Func<IGsxService, Task>> ServiceSubscriptions { get; } = [];
        public class CommBusSubscription(string name, BroadcastFlag flag, Action<string, string> callback)
        {
            public string Name { get; set; } = name;
            public BroadcastFlag Flag { get; set; } = flag;
            public Action<string, string> Callback { get; set; } = callback;
        }

        protected override async Task DoInit()
        {
            try
            {
                if (LuaEngine != null)
                    return;

                LuaEngine = new();
                CreateEnvironment();
                string code = File.ReadAllText(GetScriptPath());
                LuaChunk = LuaEngine.CompileChunk(code, FileName, new LuaCompileOptions() { ClrEnabled = false, DebugEngine = LuaExceptionDebugger.Default });
                LuaEnv.DoChunk(LuaChunk);
                GsxController.WalkaroundPreAction += BeforeWalkaroundSkip;
                GsxController.WalkaroundWasSkipped += AfterWalkaroundSkip;
                await Task.Delay(750);
                InitComplete = true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                if (ex is LuaRuntimeException luaEx)
                    Logger.Error($"{FileName} Line {luaEx.Line}");
                if (ex is LuaParseException parseEx)
                    Logger.Error($"{FileName} Line {parseEx.Line}");
            }
        }

        protected virtual void ExecuteCode(string code)
        {
            try
            {
                if (LuaEngine == null)
                    return;

                LuaEnv.DoChunk(code, FileName);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                if (ex is LuaRuntimeException luaEx)
                    Logger.Error($"{FileName} Line {luaEx.Line}");
                if (ex is LuaParseException parseEx)
                    Logger.Error($"{FileName} Line {parseEx.Line}");
            }
        }

        protected virtual bool HasLuaFunction(string name)
        {
            return HasLuaFunction(name, out _);
        }

        protected virtual bool HasLuaFunction(string name, out LuaFunction luaFunc)
        {
            if (LuaEnv == null)
            {
                Logger.Warning($"Lua Environment not loaded!");
                luaFunc = null;
                return false;
            }

            dynamic lua = LuaEnv;
            dynamic func = lua[name];
            if (func != null)
                luaFunc = new(name, func);
            else
                luaFunc = null;

            return luaFunc != null;
        }

        protected virtual T CallLua<T>(LuaFunction func)
        {
            try
            {
                return (T)(func?.Function?.Invoke());
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in Lua Function '{func?.Name}':");
                Logger.LogException(ex);
                if (ex is LuaRuntimeException luaEx)
                    Logger.Error($"{FileName} Line {luaEx.Line}");
                if (ex is LuaParseException parseEx)
                    Logger.Error($"{FileName} Line {parseEx.Line}");
            }

            return default;
        }

        protected virtual T CallLua<T>(LuaFunction func, dynamic param0)
        {
            try
            {
                return (T)(func?.Function?.Invoke(param0));
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in Lua Function '{func?.Name}':");
                Logger.LogException(ex);
                if (ex is LuaRuntimeException luaEx)
                    Logger.Error($"{FileName} Line {luaEx.Line}");
                if (ex is LuaParseException parseEx)
                    Logger.Error($"{FileName} Line {parseEx.Line}");
            }

            return default;
        }

        protected virtual T CallLua<T>(LuaFunction func, dynamic param0, dynamic param1)
        {
            try
            {
                return (T)(func?.Function?.Invoke(param0, param1));
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in Lua Function '{func?.Name}':");
                Logger.LogException(ex);
                if (ex is LuaRuntimeException luaEx)
                    Logger.Error($"{FileName} Line {luaEx.Line}");
                if (ex is LuaParseException parseEx)
                    Logger.Error($"{FileName} Line {parseEx.Line}");
            }

            return default;
        }

        protected virtual T CallLua<T>(LuaFunction func, dynamic param0, dynamic param1, dynamic param2)
        {
            try
            {
                return (T)(func?.Function?.Invoke(param0, param1, param2));
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in Lua Function '{func?.Name}':");
                Logger.LogException(ex);
                if (ex is LuaRuntimeException luaEx)
                    Logger.Error($"{FileName} Line {luaEx.Line}");
                if (ex is LuaParseException parseEx)
                    Logger.Error($"{FileName} Line {parseEx.Line}");
            }

            return default;
        }

        protected virtual void CreateEnvironment()
        {
            LuaEnv = LuaEngine.CreateEnvironment<LuaGlobal>();
            dynamic _env = LuaEnv;
            _env.GetSetting = new Func<string, dynamic>(GetSetting);
            _env.GetSettingProfile = new Func<ISettingProfile>(() => AppService.Instance?.ISettingProfile);
            _env.GetAircraftPlugin = new Func<AircraftBase>(() => AppService.Instance?.AircraftController?.Aircraft);
            _env.GetGsxController = new Func<IGsxController>(() => AppService.Instance?.GsxController);
            _env.UseVar = new Action<string, string>(RegisterVariable);
            _env.UseEvent = new Action<string>(RegisterEvent);
            _env.SubVar = new Action<string, string, string>(CallbackVariable);
            _env.SubEvent = new Action<string, string>(CallbackEvent);
            _env.ReadVar = new Func<string, dynamic>(ReadVariable);
            _env.ReadVarString = new Func<string, string>(ReadVariableString);
            _env.WriteVar = new Action<string, dynamic>(WriteVariable);
            _env.WriteEvent = new Action<string, dynamic[]>(WriteEvent);
            _env.SendInput = new Action<string, double>(SendInput);
            _env.SendBus = new Action<string, string, int>(SendCommBus);
            _env.SubBus = new Action<string, int, string>(RegisterCommBus);
            _env.CalculatorCode = new Action<string>(CalculatorCode);
            _env.Sleep = new Action<int>((delay) => Task.Delay(delay, Token).GetAwaiter().GetResult());
            _env.RunAfter = new Action<int, string>(RunAfter);
            _env.Log = new Action<string>(WriteDebug);
            _env.Info = new Action<string>(WriteInfo);
            _env.ToggleWalkaround = new Action(() => GsxController.ToggleWalkaround().GetAwaiter().GetResult());
            _env.AutomationState = new Func<int>(() => (int)GsxController.IAutomationController.State);
            _env.MatchAircraftString = new Func<string, bool>(MatchAircraftString);
            _env.SetPaxBoard = new Action<int>((pax) => GsxController.SetPaxBoard(pax).GetAwaiter().GetResult());
            _env.SetPaxDeboard = new Action<int>((pax) => GsxController.SetPaxDeboard(pax).GetAwaiter().GetResult());
            _env.SubGsxService = new Action<int, string>(RegisterGsxService);
        }

        protected virtual dynamic GetSetting(string key)
        {
            if (ISettingProfile.HasSetting(key, out object value))
            {
                dynamic result;
                if (value is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
                        result = element.GetBoolean();
                    else if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int intValue))
                        result = intValue;
                    else if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out double dblValue))
                        result = dblValue;
                    else if (element.ValueKind == JsonValueKind.String)
                        result = element.GetString();
                    else
                        result = value;
                }
                else
                    result = value;

                return result;
            }
            else
                return null;
        }

        protected virtual string GetScriptPath()
        {
            return Path.Join(ProductDefinition.PluginFolder, Directory, FileName);
        }

        protected virtual void RegisterVariable(string name, string unit = SimUnitType.Number)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(name) && !ResourceSubscriptions.ContainsKey(name))
                {
                    var sub = SimStore.AddVariable(name, unit);
                    if (sub != null)
                    {
                        Logger.Debug($"Added Variable Subscription for '{name}'");
                        ResourceSubscriptions.Add(name, sub);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual void RegisterEvent(string name)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(name) && !ResourceSubscriptions.ContainsKey(name))
                {
                    var sub = SimStore.AddEvent(name);
                    if (sub != null)
                    {
                        Logger.Debug($"Added Event Subscription for '{name}'");
                        ResourceSubscriptions.Add(name, sub);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual void CallbackVariable(string name, string func, string unit = SimUnitType.Number)
        {
            try
            {
                RegisterVariable(name, unit);

                if (ResourceSubscriptions.TryGetValue(name, out var sub))
                {
                    if (unit == SimUnitType.String)
                        sub.OnReceived += (sub, data) => ExecuteCode($"{func}('{sub.GetString()}')");
                    else
                        sub.OnReceived += (sub, data) => ExecuteCode($"{func}({Conversion.ToString(sub.GetNumber())})");
                    Logger.Debug($"Added Callback for '{name}' => {func}()");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual void CallbackEvent(string name, string func)
        {
            try
            {
                RegisterEvent(name);

                if (ResourceSubscriptions.TryGetValue(name, out var sub))
                    sub.OnReceived += (sub, data) => ExecuteCode($"{func}({data})");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual void RunAfter(int delayMs, string code)
        {
            _ = Task.Delay(delayMs, Token).ContinueWith((_) => ExecuteCode(code));
        }

        protected virtual dynamic ReadVariable(string name)
        {
            try
            {
                if (ResourceSubscriptions.TryGetValue(name, out var sub) && sub is SimVarSubscription subVar)
                    return subVar.GetNumber();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return null;
        }

        protected virtual string ReadVariableString(string name)
        {   
            try
            {
                if (ResourceSubscriptions.TryGetValue(name, out var sub) && sub is SimVarSubscription subVar && subVar.Resource.IsString)
                    return subVar.GetString();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return null;
        }

        protected virtual void WriteVariable(string name, dynamic value)
        {
            try
            {
                if (ResourceSubscriptions.TryGetValue(name, out var sub) && sub is SimVarSubscription subVar)
                    subVar.WriteValue(value);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual void WriteEvent(string name, dynamic[] values)
        {
            try
            {
                if (ResourceSubscriptions.TryGetValue(name, out var sub) && sub is SimEventSubscription subEvt)
                    subEvt.WriteValues(values);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual void SendInput(string name, double value)
        {
            try
            {
                Logger.Debug($"Sending Input Event '{name}' with Value: {value}");
                if (AppResources.InputEventManager.SendEvent(name, value).Result == false)
                    Logger.Warning($"Input Event '{name}' failed to execute");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual void SendCommBus(string @event, string data, int flag = (int)BroadcastFlag.DEFAULT)
        {
            try
            {
                CommBus.SendCommBus(@event, data, (BroadcastFlag)flag);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual void RegisterCommBus(string @event, int eventSource, string func)
        {
            try
            {
                var query = CommBusSubscriptions.Where(s => s.Name == @event);
                if (!query.Any())
                {
                    var callback = new Action<string, string>((evt, data) => ExecuteCode($"{func}('{evt}', '{data}')"));
                    CommBus.RegisterCommBus(@event, (BroadcastFlag)eventSource, callback);
                    CommBusSubscriptions.Add(new (@event, (BroadcastFlag)eventSource, callback));
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual void CalculatorCode(string code)
        {
            CommBus.ExecuteCalculatorCode(code);
        }

        protected virtual bool MatchAircraftString(string text)
        {
            return AppService.Instance.SimConnect.AircraftString.Contains(text, StringComparison.InvariantCultureIgnoreCase);
        }

        protected virtual void RegisterGsxService(int type, string func)
        {
            try
            {
                if (GsxController.TryGetService((GsxServiceType)type, out var service))
                {
                    var callback = new Func<IGsxService, Task>((svc) => { ExecuteCode($"{func}({(int)(svc.State)})"); return Task.CompletedTask; });
                    service.OnStateChanged += callback;
                    ServiceSubscriptions.Add((GsxServiceType)type, callback);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual async Task ScriptSleep(int msec)
        {
            await Task.Delay(msec, Token);
        }

        protected virtual void WriteDebug(string message)
        {
            WriteLog(LogLevel.Debug, message);
        }

        protected virtual void WriteInfo(string message)
        {
            WriteLog(LogLevel.Information, message);
        }

        protected virtual void WriteLog(LogLevel level, string message)
        {
            try
            {
                Logger.Log(level, message, FileName, "");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected override Task DoStop()
        {
            foreach (var sub in ResourceSubscriptions)
                SimStore?.Remove(sub.Key);
            ResourceSubscriptions.Clear();

            foreach (var bus in CommBusSubscriptions)
                CommBus.UnregisterCommBus(bus.Name, bus.Flag, bus.Callback);
            CommBusSubscriptions.Clear();

            foreach (var svc in ServiceSubscriptions)
            {
                if (GsxController.TryGetService(svc.Key, out var service))
                    service.OnStateChanged -= svc.Value;
            }
            ServiceSubscriptions.Clear();

            GsxController.WalkaroundPreAction -= BeforeWalkaroundSkip;
            GsxController.WalkaroundWasSkipped -= AfterWalkaroundSkip;

            LuaChunk?.Lua?.Clear();
            LuaChunk = null;

            if (LuaEnv != null)
            {
                LuaEnv?.Clear();
                LuaEnv = null;
            }

            if (LuaEngine != null)
            {
                LuaEngine.Clear();
                LuaEngine.Dispose();
                LuaEngine = null;
            }
            InitComplete = false;

            Logger.Debug($"Resources for Script {FileName} cleared");
            return Task.CompletedTask;
        }

        public override Task OnCouatlStarted()
        {
            if (HasLuaFunction("OnCouatlStarted", out LuaFunction func))
                CallLua<object>(func);
            return Task.CompletedTask;
        }

        public override Task RunInterval()
        {
            if (HasLuaFunction("RunInterval", out LuaFunction func))
                CallLua<object>(func);
            return Task.CompletedTask;
        }

        public override Task Stop()
        {
            if (HasLuaFunction("Stop", out LuaFunction func))
                CallLua<object>(func);
            return base.Stop();
        }

        protected override Task<bool> GetIsCargo()
        {
            if (HasLuaFunction("GetIsCargo", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetIsCargo();
        }

        protected virtual Task BeforeWalkaroundSkip()
        {
            if (HasLuaFunction("BeforeWalkaroundSkip", out LuaFunction func))
                CallLua<object>(func);
            return Task.CompletedTask;
        }

        protected virtual Task AfterWalkaroundSkip()
        {
            if (HasLuaFunction("AfterWalkaroundSkip", out LuaFunction func))
                CallLua<object>(func);
            return Task.CompletedTask;
        }

        protected override Task<bool> GetReadyDepartureServices()
        {
            if (HasLuaFunction("GetReadyDepartureServices", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetReadyDepartureServices();
        }

        public override Task<bool> GetSmartButtonRequest()
        {
            if (HasLuaFunction("GetSmartButtonRequest", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetSmartButtonRequest();
        }

        public override Task ResetSmartButton()
        {
            if (HasLuaFunction("ResetSmartButton", out LuaFunction func))
            {
                CallLua<object>(func);
                return Task.CompletedTask;
            }
            else
                return base.ResetSmartButton();
        }

        public override Task<DisplayUnit> GetAircraftUnits()
        {
            if (HasLuaFunction("GetAircraftUnits", out LuaFunction func))
                return Task.FromResult(CallLua<DisplayUnit>(func));
            else
                return base.GetAircraftUnits();
        }

        protected override Task<int> GetSpeed()
        {
            if (HasLuaFunction("GetSpeed", out LuaFunction func))
                return Task.FromResult(CallLua<int>(func));
            else
                return base.GetSpeed();
        }

        protected override Task<bool> GetEngine1()
        {
            if (HasLuaFunction("GetEngine1", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetEngine1();
        }

        protected override Task<bool> GetEngine2()
        {
            if (HasLuaFunction("GetEngine2", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetEngine2();
        }

        protected override Task<double> GetFuelOnBoardKg()
        {
            if (HasLuaFunction("GetFuelOnBoardKg", out LuaFunction func))
                return Task.FromResult(CallLua<double>(func));
            else
                return base.GetFuelOnBoardKg();
        }

        protected override Task<double> GetWeightTotalKg()
        {
            if (HasLuaFunction("GetWeightTotalKg", out LuaFunction func))
                return Task.FromResult(CallLua<double>(func));
            else
                return base.GetWeightTotalKg();
        }

        protected override Task<bool> GetAvionicPowered()
        {
            if (HasLuaFunction("GetAvionicPowered", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetAvionicPowered();
        }

        protected override Task<bool> GetApuRunning()
        {
            if (HasLuaFunction("GetApuRunning", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetApuRunning();
        }

        protected override Task<bool> GetApuBleedOn()
        {
            if (HasLuaFunction("GetApuBleedOn", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetApuBleedOn();
        }

        protected override Task<bool> GetExternalPowerConnected()
        {
            if (HasLuaFunction("GetExternalPowerConnected", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetExternalPowerConnected();
        }

        protected override Task<bool> GetExternalPowerAvailable()
        {
            if (HasLuaFunction("GetExternalPowerAvailable", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetExternalPowerAvailable();
        }

        public override Task<bool> GetHasFuelSynch()
        {
            if (HasLuaFunction("GetHasFuelSynch", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetHasFuelSynch();
        }

        public override Task<bool> GetCanSetPayload()
        {
            if (HasLuaFunction("GetCanSetPayload", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetCanSetPayload();
        }

        public override Task<bool> GetHasFobSaveRestore()
        {
            if (HasLuaFunction("GetHasFobSaveRestore", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetHasFobSaveRestore();
        }

        public override Task<bool> GetHasGpuInternal()
        {
            if (HasLuaFunction("GetHasGpuInternal", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetHasGpuInternal();
        }

        public override Task<bool> GetUseGpuGsx()
        {
            if (HasLuaFunction("GetUseGpuGsx", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetUseGpuGsx();
        }

        public override Task<bool> GetHasChocks()
        {
            if (HasLuaFunction("GetHasChocks", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetHasChocks();
        }

        public override Task<bool> GetHasCones()
        {
            if (HasLuaFunction("GetHasCones", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetHasCones();
        }

        public override Task<bool> GetHasPca()
        {
            if (HasLuaFunction("GetHasPca", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetHasPca();
        }

        protected override Task<bool> GetEquipmentChocks()
        {
            if (HasLuaFunction("GetEquipmentChocks", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetEquipmentChocks();
        }

        protected override Task<bool> GetEquipmentCones()
        {
            if (HasLuaFunction("GetEquipmentCones", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetEquipmentChocks();
        }

        protected override Task<bool> GetEquipmentPca()
        {
            if (HasLuaFunction("GetEquipmentPca", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetEquipmentPca();
        }

        protected override Task<bool> GetBrakeSet()
        {
            if (HasLuaFunction("GetBrakeSet", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetBrakeSet();
        }

        protected override Task<bool> GetLightNav()
        {
            if (HasLuaFunction("GetLightNav", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetLightNav();
        }

        protected override Task<bool> GetLightBeacon()
        {
            if (HasLuaFunction("GetLightBeacon", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetLightBeacon();
        }

        public override Task<bool> SetEquipmentPower(bool state, bool force = false)
        {
            if (HasLuaFunction("SetEquipmentPower", out LuaFunction func))
                CallLua<object>(func, state, force);
            return Task.FromResult(true);
        }

        public override Task SetParkingBrake(bool state)
        {
            if (HasLuaFunction("SetParkingBrake", out LuaFunction func))
                return Task.FromResult(CallLua<object>(func, state));
            else
                return base.SetParkingBrake(state);
        }

        public override Task SetEquipmentChocks(bool state, bool force = false)
        {
            if (HasLuaFunction("SetEquipmentChocks", out LuaFunction func))
                return Task.FromResult(CallLua<object>(func, state, force));
            else
                return base.SetEquipmentChocks(state, force);
        }

        public override Task SetEquipmentCones(bool state, bool force = false)
        {
            if (HasLuaFunction("SetEquipmentCones", out LuaFunction func))
                return Task.FromResult(CallLua<object>(func, state, force));
            else
                return base.SetEquipmentCones(state, force);
        }

        public override Task<bool> GetHasAirStairForward()
        {
            if (HasLuaFunction("GetHasAirStairForward", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetHasAirStairForward();
        }

        public override Task<bool> GetHasAirStairAft()
        {
            if (HasLuaFunction("GetHasAirStairAft", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetHasAirStairAft();
        }

        protected override Task<bool> GetHasOpenDoors()
        {
            if (HasLuaFunction("GetHasOpenDoors", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetHasOpenDoors();
        }

        public override Task DoorsAllClose()
        {
            if (HasLuaFunction("DoorsAllClose", out LuaFunction func))
            {
                CallLua<object>(func);
                return Task.CompletedTask;
            }
            else
                return base.DoorsAllClose();
        }

        public override Task SetEquipmentPca(bool state, bool force = false)
        {
            if (HasLuaFunction("SetEquipmentPca", out LuaFunction func))
            {
                CallLua<object>(func, state, force);
                return Task.CompletedTask;
            }
            else
                return base.SetEquipmentPca(state, force);
        }

        public override Task OnAutomationStateChange(AutomationState state)
        {
            if (HasLuaFunction("OnAutomationStateChange", out LuaFunction func))
            {
                CallLua<object>(func, (int)state);
                return Task.CompletedTask;
            }
            else
                return base.OnAutomationStateChange(state);
        }

        public override Task OnDoorTrigger(GsxDoor door, bool trigger)
        {
            if (HasLuaFunction("OnDoorTrigger", out LuaFunction func))
            {
                CallLua<object>(func, (int)door, trigger);
                return Task.CompletedTask;
            }
            else
                return base.OnDoorTrigger(door, trigger);
        }

        public override Task OnJetwayChange(GsxServiceState state)
        {
            if (HasLuaFunction("OnJetwayChange", out LuaFunction func))
            {
                CallLua<object>(func, (int)state);
                return Task.CompletedTask;
            }
            else
                return base.OnJetwayChange(state);
        }

        public override Task OnStairChange(GsxServiceState state)
        {
            if (HasLuaFunction("OnStairChange", out LuaFunction func))
            {
                CallLua<object>(func, (int)state);
                return Task.CompletedTask;
            }
            else
                return base.OnStairChange(state);
        }

        public override Task OnStairOperationChange(GsxServiceState state)
        {
            if (HasLuaFunction("OnStairOperationChange", out LuaFunction func))
            {
                CallLua<object>(func, (int)state);
                return Task.CompletedTask;
            }
            else
                return base.OnStairOperationChange(state);
        }

        public override Task<bool> GetIsFuelOnStairSide()
        {
            if (HasLuaFunction("GetIsFuelOnStairSide", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetIsFuelOnStairSide();
        }

        public override Task RefuelActive()
        {
            if (HasLuaFunction("RefuelActive", out LuaFunction func))
            {
                CallLua<object>(func);
                return Task.CompletedTask;
            }
            else
                return base.RefuelActive();
        }

        public override Task RefuelStart(double fuelTargetKg)
        {
            if (HasLuaFunction("RefuelStart", out LuaFunction func))
            {
                CallLua<object>(func, fuelTargetKg);
                return Task.CompletedTask;
            }
            else
                return base.RefuelStart(fuelTargetKg);
        }

        public override Task SetFuelOnBoardKg(double fuelKg)
        {
            if (HasLuaFunction("SetFuelOnBoardKg", out LuaFunction func))
            {
                CallLua<object>(func, fuelKg);
                return Task.CompletedTask;
            }
            else
                return base.SetFuelOnBoardKg(fuelKg);
        }

        public override Task RefuelTick(double stepKg, double fuelOnBoardKg)
        {
            if (HasLuaFunction("RefuelTick", out LuaFunction func))
            {
                CallLua<object>(func, stepKg, fuelOnBoardKg);
                return Task.CompletedTask;
            }
            else
                return base.RefuelTick(stepKg, fuelOnBoardKg);
        }

        public override Task RefuelCompleted()
        {
            if (HasLuaFunction("RefuelCompleted", out LuaFunction func))
            {
                CallLua<object>(func);
                return Task.CompletedTask;
            }
            else
                return base.RefuelCompleted();
        }

        public override Task RefuelStop(double fuelTargetKg, bool setTarget)
        {
            if (HasLuaFunction("RefuelStop", out LuaFunction func))
            {
                CallLua<object>(func, fuelTargetKg, setTarget);
                return Task.CompletedTask;
            }
            else
                return base.RefuelStop(fuelTargetKg, setTarget);
        }

        public override Task SetPayloadEmpty()
        {
            if (HasLuaFunction("SetPayloadEmpty", out LuaFunction func))
            {
                CallLua<object>(func);
                return Task.CompletedTask;
            }
            else
                return base.SetPayloadEmpty();
        }

        public override Task PushStateChange(GsxServiceState state)
        {
            if (HasLuaFunction("PushStateChange", out LuaFunction func))
            {
                CallLua<object>(func, (int)state);
                return Task.CompletedTask;
            }
            else
                return base.PushStateChange(state);
        }

        public override Task PushOperationChange(int status)
        {
            if (HasLuaFunction("PushOperationChange", out LuaFunction func))
            {
                CallLua<object>(func, status);
                return Task.CompletedTask;
            }
            else
                return base.PushOperationChange(status);
        }

        public override Task BoardActive(int paxTarget, double cargoTargetKg)
        {
            if (HasLuaFunction("BoardActive", out LuaFunction func))
            {
                CallLua<object>(func, paxTarget, cargoTargetKg);
                return Task.CompletedTask;
            }
            else
                return base.BoardActive(paxTarget, cargoTargetKg);
        }

        public override Task BoardChangePax(int paxOnBoard, double weightPerPaxKg)
        {
            if (HasLuaFunction("BoardChangePax", out LuaFunction func))
            {
                CallLua<object>(func, paxOnBoard, weightPerPaxKg);
                return Task.CompletedTask;
            }
            else
                return base.BoardChangePax(paxOnBoard, weightPerPaxKg);
        }

        public override Task SetCargoCrew(int paxOnBoard, double weightPerPaxKg)
        {
            base.SetCargoCrew(paxOnBoard, weightPerPaxKg);
            
            if (HasLuaFunction("SetCargoCrew", out LuaFunction func))
                CallLua<object>(func, paxOnBoard, weightPerPaxKg);
            
            return Task.CompletedTask;
        }

        public override Task BoardChangeCargo(int progressLoad, double cargoOnBoardKg)
        {
            if (HasLuaFunction("BoardChangeCargo", out LuaFunction func))
            {
                CallLua<object>(func, progressLoad, cargoOnBoardKg);
                return Task.CompletedTask;
            }
            else
                return base.BoardChangeCargo(progressLoad, cargoOnBoardKg);
            
        }

        public override Task BoardLoadingChange(GsxDoor door, bool state)
        {
            if (HasLuaFunction("BoardLoadingChange", out LuaFunction func))
            {
                CallLua<object>(func, (int)door, state);
                return Task.CompletedTask;
            }
            else
                return base.BoardLoadingChange(door, state);
        }

        protected override Task<bool> GetIsBoardingCompleted()
        {
            if (HasLuaFunction("GetIsBoardingCompleted", out LuaFunction func))
                return Task.FromResult(CallLua<bool>(func));
            else
                return base.GetIsBoardingCompleted();
        }

        public override Task BoardCompleted(int paxTarget, double weightPerPaxKg, double cargoTargetKg)
        {
            if (HasLuaFunction("BoardCompleted", out LuaFunction func))
                CallLua<object>(func, paxTarget, weightPerPaxKg, cargoTargetKg);

            return base.BoardCompleted(paxTarget, weightPerPaxKg, cargoTargetKg);
        }

        public override Task DeboardActive()
        {
            if (HasLuaFunction("DeboardActive", out LuaFunction func))
            {
                CallLua<object>(func);
                return Task.CompletedTask;
            }
            else
                return base.DeboardActive();
        }

        public override Task DeboardChangePax(int paxOnBoard, int gsxTotal, double weightPerPaxKg)
        {
            if (HasLuaFunction("DeboardChangePax", out LuaFunction func))
            {
                CallLua<object>(func, paxOnBoard, gsxTotal, weightPerPaxKg);
                return Task.CompletedTask;
            }
            else
                return base.DeboardChangePax(paxOnBoard, gsxTotal, weightPerPaxKg);
        }

        public override Task DeboardChangeCargo(int progressUnload, double cargoOnBoardKg)
        {
            if (HasLuaFunction("DeboardChangeCargo", out LuaFunction func))
            {
                CallLua<object>(func, progressUnload, cargoOnBoardKg);
                return Task.CompletedTask;
            }
            else
                return base.DeboardChangeCargo(progressUnload, cargoOnBoardKg);
        }

        public override Task DeboardUnloadingChange(GsxDoor door, bool state)
        {
            if (HasLuaFunction("DeboardUnloadingChange", out LuaFunction func))
            {
                CallLua<object>(func, (int)door, state);
                return Task.CompletedTask;
            }
            else
                return base.DeboardUnloadingChange(door, state);
        }

        public override Task DeboardCompleted()
        {
            if (HasLuaFunction("DeboardCompleted", out LuaFunction func))
                CallLua<object>(func);
            return base.DeboardCompleted();
        }
    }
}
