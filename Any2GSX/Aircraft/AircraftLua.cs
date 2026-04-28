using Any2GSX.AppConfig;
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
using System.Runtime.CompilerServices;
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
        protected virtual ConcurrentDictionary<string, EventCallback> EventCallbacks { get; } = [];
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

        protected virtual bool HasLuaFunction([CallerMemberName] string name = "")
        {
            return HasLuaFunction(out _, name);
        }

        protected virtual bool HasLuaFunction(out LuaFunction luaFunc, [CallerMemberName] string name = "")
        {
            luaFunc = null;
            if (LuaEnv == null)
            {
                Logger.Warning($"Lua Environment not loaded! (Function '{name}')");
                return false;
            }
            if (string.IsNullOrWhiteSpace(name))
                return false;

            dynamic lua = LuaEnv;
            dynamic func = lua[name];
            if (func != null)
                luaFunc = new(name, func);
            else
                luaFunc = null;

            return luaFunc != null;
        }

        protected virtual Task CallLua(LuaFunction func)
        {
            try
            {
                func?.Function?.Invoke();
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

            return Task.CompletedTask;
        }

        protected virtual Task<T> CallLua<T>(LuaFunction func)
        {
            try
            {
                return Task.FromResult((T)(func?.Function?.Invoke()));
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

            return Task.FromResult(default(T));
        }

        protected virtual Task<T> CallLua<T>(LuaFunction func, dynamic param0)
        {
            try
            {
                return Task.FromResult((T)(func?.Function?.Invoke(param0)));
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

            return Task.FromResult(default(T));
        }

        protected virtual Task<T> CallLua<T>(LuaFunction func, dynamic param0, dynamic param1)
        {
            try
            {
                return Task.FromResult((T)(func?.Function?.Invoke(param0, param1)));
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

            return Task.FromResult(default(T));
        }

        protected virtual Task<T> CallLua<T>(LuaFunction func, dynamic param0, dynamic param1, dynamic param2)
        {
            try
            {
                return Task.FromResult((T)(func?.Function?.Invoke(param0, param1, param2)));
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

            return Task.FromResult(default(T));
        }

        protected virtual Task<T> CallLua<T>(LuaFunction func, dynamic param0, dynamic param1, dynamic param2, dynamic param3)
        {
            try
            {
                return Task.FromResult((T)(func?.Function?.Invoke(param0, param1, param2, param3)));
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

            return Task.FromResult(default(T));
        }

        protected virtual void CreateEnvironment()
        {
            LuaEnv = LuaEngine.CreateEnvironment<LuaGlobal>();
            dynamic _env = LuaEnv;
            _env.GetSetting = new Func<string, dynamic>(GetSetting);
            _env.GetGenericSetting = new Func<string, dynamic>(GetGenericSetting);
            _env.GetPluginSetting = new Func<string, dynamic>(GetPluginSetting);
            _env.GetSettingProfile = new Func<ISettingProfile>(() => AppService.Instance?.ISettingProfile);
            _env.GetAircraftPlugin = new Func<AircraftBase>(() => AppService.Instance?.AircraftController?.Aircraft);
            _env.GetGsxController = new Func<IGsxController>(() => AppService.Instance?.GsxController);
            _env.FuelWeightKgPerGallon = new Func<double>(() => AppService.Instance?.AircraftController?.FuelWeightKgPerGallon ?? 3.03907);
            _env.UseVar = new Action<string, string>(RegisterVariable);
            _env.UseEvent = new Action<string>(RegisterEvent);
            _env.SubVar = new Action<string, string, string>(CallbackVariable);
            _env.SubEvent = new Action<string, string>(CallbackEvent);
            _env.ReadVar = new Func<string, dynamic>(ReadVariable);
            _env.ReadVarString = new Func<string, string>(ReadVariableString);
            _env.WriteVar = new Func<string, dynamic, Task>(WriteVariable);
            _env.WriteEvent = new Func<string, dynamic[], Task>(WriteEvent);
            _env.SendInput = new Func<string, double, Task>(SendInput);
            _env.SendBus = new Func<string, string, int, Task>(SendCommBus);
            _env.SubBus = new Func<string, int, string, Task>(RegisterCommBus);
            _env.CalculatorCode = new Func<string, Task>(CalculatorCode);
            _env.Sleep = new Action<int>((delay) => Task.Delay(delay, Token).RunSync());
            _env.RunAfter = new Action<int, string>(RunAfter);
            _env.Log = new Action<string>(WriteDebug);
            _env.Info = new Action<string>(WriteInfo);
            _env.ToggleWalkaround = new Func<Task>(() => GsxController.ToggleWalkaround());
            _env.AutomationState = new Func<int>(() => (int)GsxController.IAutomationController.State);
            _env.MatchAircraftString = new Func<string, bool>(MatchAircraftString);
            _env.SetPaxBoard = new Func<int, Task>((pax) => GsxController.SetPaxBoard(pax));
            _env.SetPaxDeboard = new Func<int, Task>((pax) => GsxController.SetPaxDeboard(pax));
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

        protected virtual dynamic GetGenericSetting(string key)
        {
            return GetSetting($"{SettingProfile.GenericIdUpper}.Option.{key}");
        }

        protected virtual dynamic GetPluginSetting(string key)
        {
            return GetSetting(AppService.Instance.SettingProfile.GetPluginKey(key));
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
                    var handler = new EventCallback(name, SimUnitType.Number, func, DoEvent, sub);
                    EventCallbacks.Add(name, handler);
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
                {
                    var handler = new EventCallback(name, SimUnitType.Number, func, DoEvent, sub);
                    EventCallbacks.Add(name, handler);
                    Logger.Debug($"Added Callback for '{name}' => {func}()");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual Task DoEvent(ISimResourceSubscription sub, string func, string unit = SimUnitType.Number)
        {
            if (unit == SimUnitType.String)
                ExecuteCode($"{func}('{sub.GetString()}')");
            else
                ExecuteCode($"{func}({Conversion.ToString(sub.GetNumber())})");

            return Task.CompletedTask;
        }

        protected virtual void RunAfter(int delayMs, string code)
        {
            _ = TaskTools.RunDelayed(() => ExecuteCode(code), delayMs, Token);
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

        protected virtual Task WriteVariable(string name, dynamic value)
        {
            try
            {
                if (ResourceSubscriptions.TryGetValue(name, out var sub) && sub is SimVarSubscription subVar)
                    return subVar.WriteValue(value);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return Task.CompletedTask;
        }

        protected virtual Task WriteEvent(string name, dynamic[] values)
        {
            try
            {
                if (ResourceSubscriptions.TryGetValue(name, out var sub) && sub is SimEventSubscription subEvt)
                    return subEvt.WriteValues(values);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return Task.CompletedTask;
        }

        protected virtual async Task SendInput(string name, double value)
        {
            try
            {
                Logger.Debug($"Sending Input Event '{name}' with Value: {value}");
                if (!await AppResources.InputEventManager.SendEvent(name, value))
                    Logger.Warning($"Input Event '{name}' failed to execute");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual Task SendCommBus(string @event, string data, int flag = (int)BroadcastFlag.DEFAULT)
        {
            try
            {
                return CommBus.SendCommBus(@event, data, (BroadcastFlag)flag);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return Task.CompletedTask;
        }

        protected virtual async Task RegisterCommBus(string @event, int eventSource, string func)
        {
            try
            {
                var query = CommBusSubscriptions.Where(s => s.Name == @event);
                if (!query.Any())
                {
                    var callback = new Action<string, string>((evt, data) => ExecuteCode($"{func}('{evt}', '{data}')"));
                    await CommBus.RegisterCommBus(@event, (BroadcastFlag)eventSource, callback);
                    CommBusSubscriptions.Add(new(@event, (BroadcastFlag)eventSource, callback));
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual Task CalculatorCode(string code)
        {
            return CommBus.ExecuteCalculatorCode(code);
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
            foreach (var evt in EventCallbacks)
                evt.Value.Unsubscribe();
            EventCallbacks.Clear();

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
            if (HasLuaFunction(out LuaFunction func))
                return CallLua(func);
            else
                return base.OnCouatlStarted();
        }

        public override Task OnAutomationStateChange(AutomationState state)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, state);
            else
                return base.OnAutomationStateChange(state);
        }

        public override Task CheckConnection()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua(func);
            else
                return Task.CompletedTask;
        }

        public override Task RunInterval()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua(func);
            else
                return Task.CompletedTask;
        }

        public override Task<bool> GetIsCargo()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetIsCargo();
        }

        public override Task<int> GetSpeed()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<int>(func);
            else
                return base.GetSpeed();
        }

        public override Task<bool> GetEngine1()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetEngine1();
        }

        public override Task<bool> GetEngine2()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetEngine2();
        }

        public override Task<bool> GetEngineRunning()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetEngineRunning();
        }

        public override Task<bool> GetReadyDepartureServices()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetReadyDepartureServices();
        }

        public override Task<bool> GetSmartButtonRequest()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetSmartButtonRequest();
        }

        public override Task ResetSmartButton()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua(func);
            else
                return base.ResetSmartButton();
        }

        public override Task<DisplayUnit> GetAircraftUnits()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<DisplayUnit>(func);
            else
                return base.GetAircraftUnits();
        }

        public override Task NotifyCockpit(CockpitNotification notification)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, notification);
            else
                return base.NotifyCockpit(notification);
        }

        public override Task<double> GetFuelOnBoardKg()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<double>(func);
            else
                return base.GetFuelOnBoardKg();
        }

        public override Task<double> GetWeightTotalKg()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<double>(func);
            else
                return base.GetWeightTotalKg();
        }

        public override Task<double> GetWeightZeroFuelKg()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<double>(func);
            else
                return base.GetWeightZeroFuelKg();
        }

        public override Task<bool> GetAvionicPowered()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetAvionicPowered();
        }

        public override Task<bool> GetApuRunning()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetApuRunning();
        }

        public override Task<bool> GetApuBleedOn()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetApuBleedOn();
        }

        public override Task<bool> GetExternalPowerConnected()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetExternalPowerConnected();
        }

        public override Task<bool> GetExternalPowerAvailable()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetExternalPowerAvailable();
        }

        public override Task<bool> GetHasFobSaveRestore()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetHasFobSaveRestore();
        }

        public override Task<bool> GetHasFuelSync()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetHasFuelSync();
        }

        public override Task<bool> GetCanSetPayload()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetCanSetPayload();
        }

        public override Task<bool> GetIsFuelOnStairSide()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetIsFuelOnStairSide();
        }

        public override Task<bool> GetHasGpuInternal()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetHasGpuInternal();
        }

        public override Task<bool> GetGpuRequireChocks()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetGpuRequireChocks();
        }

        public override Task<GsxGpuUsage> GetUseGpuGsx()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<GsxGpuUsage>(func);
            else
                return base.GetUseGpuGsx();
        }

        public override Task<bool> GetSettingAutoMode()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetSettingAutoMode();
        }

        public override Task<bool> GetSettingProgRefuel()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetSettingProgRefuel();
        }

        public override Task<bool> GetSettingDetectCustFuel()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetSettingDetectCustFuel();
        }

        public override Task<bool> GetSettingAdvAutomation()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetSettingAdvAutomation();
        }

        public override Task<bool> GetSettingFuelDialog()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetSettingFuelDialog();
        }

        public override Task<bool> GetHasChocks()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetHasChocks();
        }

        public override Task<bool> GetHasCones()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetHasCones();
        }

        public override Task<bool> GetHasPca()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetHasPca();
        }

        public override Task<bool> GetPcaRequirePower()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetPcaRequirePower();
        }

        public override Task<bool> GetEquipmentChocks()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetEquipmentChocks();
        }

        public override Task<bool> GetEquipmentCones()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetEquipmentCones();
        }

        public override Task<bool> GetEquipmentPca()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetEquipmentPca();
        }

        public override Task BeforeWalkaroundSkip()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua(func);
            else
                return base.BeforeWalkaroundSkip();
        }

        public override Task AfterWalkaroundSkip()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua(func);
            else
                return base.AfterWalkaroundSkip();
        }

        public override Task HandleWalkaroundEquipment()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua(func);
            else
                return base.HandleWalkaroundEquipment();
        }


        public override Task<bool> GetBrakeSet()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetBrakeSet();
        }

        public override Task<bool> GetLightNav()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetLightNav();
        }

        public override Task<bool> GetLightBeacon()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetLightBeacon();
        }

        public override Task SetParkingBrake(bool state)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, state);
            else
                return base.SetParkingBrake(state);
        }

        public override Task SetExternalPowerAvailable(bool state)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, state);
            else
                return base.SetExternalPowerAvailable(state);
        }

        public override Task SetEquipmentPower(bool state, bool force = false)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, state, force);
            else
                return base.SetEquipmentPower(state, force);
        }

        public override Task SetEquipmentChocks(bool state, bool force = false)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, state, force);
            else
                return base.SetEquipmentChocks(state, force);
        }

        public override Task SetEquipmentCones(bool state, bool force = false)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, state, force);
            else
                return base.SetEquipmentCones(state, force);
        }

        public override Task SetEquipmentPca(bool state, bool force = false)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, state, force);
            else
                return base.SetEquipmentPca(state, force);
        }

        public override Task<bool> GetHasOpenDoors()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetHasOpenDoors();
        }

        public override Task SetCargoDoors(bool state, bool force = false)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, state, force);
            else
                return base.SetCargoDoors(state, force);
        }

        public override Task DoorsAllClose()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua(func);
            else
                return base.DoorsAllClose();
        }

        public override Task<bool> GetHasAirStairForward()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetHasAirStairForward();
        }

        public override Task<bool> GetHasAirStairAft()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetHasAirStairAft();
        }

        public override Task OnDoorTrigger(GsxDoor door, bool trigger)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, door, trigger);
            else
                return base.OnDoorTrigger(door, trigger);
        }

        public override Task SetPanelLavatory(bool target)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, target);
            else
                return base.SetPanelLavatory(target);
        }

        public override Task SetPanelWater(bool target)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, target);
            else
                return base.SetPanelWater(target);
        }

        public override Task OnLoaderAttached(GsxDoor door, bool attached)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, door, attached);
            else
                return base.OnLoaderAttached(door, attached);
        }

        public override Task OnJetwayStateChange(GsxServiceState state, bool paxDoorAllowed)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, state, paxDoorAllowed);
            else
                return base.OnJetwayStateChange(state, paxDoorAllowed);
        }

        public override Task OnJetwayOperationChange(GsxServiceState state, bool paxDoorAllowed)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, state, paxDoorAllowed);
            else
                return base.OnJetwayOperationChange(state, paxDoorAllowed);
        }

        public override Task OnStairStateChange(GsxServiceState state, bool paxDoorAllowed)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, state, paxDoorAllowed);
            else
                return base.OnStairStateChange(state, paxDoorAllowed);
        }

        public override Task OnStairOperationChange(GsxServiceState state, bool paxDoorAllowed)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, state, paxDoorAllowed);
            else
                return base.OnStairOperationChange(state, paxDoorAllowed);
        }

        public override Task OnStairVerhicleChange(GsxVehicleStair stair, GsxVehicleStairState state, bool paxDoorAllowed)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, stair, state, paxDoorAllowed);
            else
                return base.OnStairVerhicleChange(stair, state, paxDoorAllowed);
        }

        public override Task SetPanelRefuel(bool target)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, target);
            else
                return base.SetPanelRefuel(target);
        }

        public override Task SetFuelOnBoardKg(double fuelOnBoardKg, double targetKg)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, fuelOnBoardKg, targetKg);
            else
                return base.SetFuelOnBoardKg(fuelOnBoardKg, targetKg);
        }

        public override Task RefuelActive()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua(func);
            else
                return base.RefuelActive();
        }

        public override Task RefuelStart(double fuelTargetKg)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, fuelTargetKg);
            else
                return base.RefuelStart(fuelTargetKg);
        }

        public override Task RefuelTick(bool isFuelInc, double stepKg, double fuelOnBoardKg, double fuelTargetKg)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, isFuelInc, stepKg, fuelOnBoardKg, fuelTargetKg);
            else
                return base.RefuelTick(isFuelInc, stepKg, fuelOnBoardKg, fuelTargetKg);
        }

        public override Task RefuelStop(double fuelTargetKg, bool setTarget)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, fuelTargetKg, setTarget);
            else
                return base.RefuelStop(fuelTargetKg, setTarget);
        }

        public override Task RefuelCompleted()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua(func);
            else
                return base.RefuelCompleted();
        }

        public override Task SetPayloadEmpty()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua(func);
            else
                return base.SetPayloadEmpty();
        }

        public override Task PushStateChange(GsxServiceState state)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, state);
            else
                return base.PushStateChange(state);
        }

        public override Task PushOperationChange(int status)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, status);
            else
                return base.PushOperationChange(status);
        }

        public override Task<int> GetPaxOnBoard()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<int>(func);
            else
                return base.GetPaxOnBoard();
        }

        public override Task<int> GetBagsOnBoard()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<int>(func);
            else
                return base.GetBagsOnBoard();
        }

        public override Task SetPaxOnBoard(int paxOnBoard, double weightPerPaxKg, int paxTarget)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, paxOnBoard, weightPerPaxKg, paxTarget);
            else
                return base.SetPaxOnBoard(paxOnBoard, weightPerPaxKg, paxTarget);
        }

        public override Task<double> GetCargoOnBoard()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<double>(func);
            else
                return base.GetCargoOnBoard();
        }

        public override Task SetCargoOnBoard(double cargoOnBoardKg, double cargoTargetKg)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, cargoOnBoardKg, cargoTargetKg);
            else
                return base.SetCargoOnBoard(cargoOnBoardKg, cargoTargetKg);
        }

        public override Task BoardRequested(int paxTarget, double cargoTargetKg)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, paxTarget, cargoTargetKg);
            else
                return base.BoardRequested(paxTarget, cargoTargetKg);
        }

        public override Task BoardActive(int paxTarget, double cargoTargetKg)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, paxTarget, cargoTargetKg);
            else
                return base.BoardActive(paxTarget, cargoTargetKg);
        }

        public override Task BoardChangePax(int paxOnBoard, double weightPerPaxKg, int paxTarget)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, paxOnBoard, weightPerPaxKg, paxTarget);
            else
                return base.BoardChangePax(paxOnBoard, weightPerPaxKg, paxTarget);
        }

        public override Task BoardChangeCargo(int progressLoad, double cargoOnBoardKg, double cargoPlannedKg)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, progressLoad, cargoOnBoardKg, cargoPlannedKg);
            else
                return base.BoardChangeCargo(progressLoad, cargoOnBoardKg, cargoPlannedKg);
        }

        public override Task<bool> GetIsBoardingCompleted()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<bool>(func);
            else
                return base.GetIsBoardingCompleted();
        }

        public override Task BoardCompleted(int paxTarget, double weightPerPaxKg, double cargoTargetKg)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, paxTarget, weightPerPaxKg, cargoTargetKg);
            else
                return base.BoardCompleted(paxTarget, weightPerPaxKg, cargoTargetKg);
        }

        public override Task DeboardRequested()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua(func);
            else
                return base.DeboardRequested();
        }

        public override Task DeboardActive()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua(func);
            else
                return base.DeboardActive();
        }

        public override Task DeboardChangePax(int paxOnBoard, int gsxTotal, double weightPerPaxKg)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, paxOnBoard, gsxTotal, weightPerPaxKg);
            else
                return base.DeboardChangePax(paxOnBoard, gsxTotal, weightPerPaxKg);
        }

        public override Task DeboardChangeCargo(int progressUnload, double cargoOnBoardKg)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, progressUnload, cargoOnBoardKg);
            else
                return base.DeboardChangeCargo(progressUnload, cargoOnBoardKg);
        }

        public override Task DeboardCompleted()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua(func);
            else
                return base.DeboardCompleted();
        }

        public override Task OnFlightplanImport(IFlightplan ofp)
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua<object>(func, ofp);
            else
                return base.OnFlightplanImport(ofp);
        }

        public override Task OnFlightplanUnload()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua(func);
            else
                return base.OnFlightplanUnload();
        }

        public override Task GenerateLoadsheetPrelim()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua(func);
            else
                return base.GenerateLoadsheetPrelim();
        }

        public override Task GenerateLoadsheetFinal()
        {
            if (HasLuaFunction(out LuaFunction func))
                return CallLua(func);
            else
                return base.GenerateLoadsheetFinal();
        }
    }

    public class EventCallback
    {
        public string Name { get; set; }
        public string Unit { get; set; }
        public string Function { get; set; }
        public Func<ISimResourceSubscription, object, Task> CallBackHandler { get; set; }
        public ISimResourceSubscription Subscription { get; set; }

        public EventCallback(string name, string unit, string function, Func<ISimResourceSubscription, string, string, Task> doEvent, ISimResourceSubscription sub)
        {
            Name = name;
            Unit = unit;
            Function = function;
            Subscription = sub;
            CallBackHandler = (s, d) => doEvent(Subscription, Function, Unit);
            Subscribe();
        }

        public virtual void Subscribe()
        {
            Subscription.OnReceived += CallBackHandler;
        }

        public virtual void Unsubscribe()
        {
            Subscription.OnReceived -= CallBackHandler;
        }
    }
}
