using Any2GSX.Aircraft;
using Any2GSX.GSX;
using Any2GSX.GSX.Automation;
using Any2GSX.GSX.Menu;
using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.Tools;
using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Any2GSX.API
{
    /// {
    ///     "request": "setPaxCount" | "setBagCount" | "setPaxDiff" | "setFuelPlanned" | "sendFinal" | "smartCall"
    ///     "data": int | int | int | int | empty | empty
    /// }
    /// {
    ///     "request": "setExternal",
    ///     "data": bool
    ///     "handler": string (optional)
    /// }
    /// {
    ///     "request": "callService",
    ///     "data":
    ///     {
    ///         "service": string (GsxServiceType Name)
    ///     }   
    /// }
    /// {
    ///     "request": "queueService",
    ///     "data":
    ///     {
    ///         "service": string, (GsxServiceType Name)
    ///         "activation": string, (GsxServiceActivation Name)
    ///         "timeDeparture": string (hh:mm:ss (TimeSpan) when activation == DepartureTime)
    ///         "serviceDependency": string, (GsxServiceDependency Name when activation == ServiceDependent)
    ///         "dependencyScope": string, (GsxServiceDependencyScope Name when activation == ServiceDependent)
    ///      }   
    /// }
    /// {
    ///     "request": "cancelService",
    ///     "data":
    ///     {
    ///         "service": string (GsxServiceType Name)
    ///     } 
    /// }
    /// {
    ///     "request": "setSavedFob",
    ///     "data":
    ///     {
    ///         "title": string, (Title/Livery as reported by App)
    ///         "fuel": int, (Value in kg)
    ///         "set": bool
    ///      }   
    /// }
    /// {
    ///     "request": "setPushDirection",
    ///     "data": string (empty/null clears pre--selection)
    /// }
    /// {
    ///     "request": "callPushback",
    ///     "data": string (optional for automatic selection - contains check) 
    /// }
    /// {
    ///     "request": "setGateArrival",
    ///     "data": string|int (passed as-is to GSX' gate.select API Method)  
    /// }
    /// {
    ///     "request": "setProfile",
    ///     "data":
    ///     {
    ///         "key": string, (exact Property Name as seen in SettingProfile)
    ///         "value": any (the matching Type)
    ///     }   
    /// }
    /// {
    ///     "request": "getProfile",
    ///     "data":
    ///     {
    ///         "key": string (exact Property Name as seen in SettingProfile - returns Object/Dictionary with { "<key>": value })
    ///     }   
    /// }

    public class ApiServer() : AppServer
    {
        public override List<string> SupportedVersions => ["v1"];
        protected virtual Dictionary<ApiRequestType, Func<RequestPayload, RequestEnvelope, Task>> RequestHandlers { get; } = [];
        public virtual GsxController GsxController => AppService.Instance.GsxController;
        public virtual GsxMenu GsxMenu => GsxController.Menu;
        public virtual GsxAutomationController AutomationController => GsxController.AutomationController;
        public virtual AircraftController AircraftController => AppService.Instance.AircraftController;
        public virtual Flightplan Flightplan => AppService.Instance.Flightplan;

        protected override void RegisterEndpoints()
        {
            SupportedEndpoints["v1"].Add("external", ProcessExternalCommand);
            SupportedEndpoints["v1"].Add("status", ProcessStatusRequest);
            SupportedEndpoints["v1"].Add("pushback", ProcessPushbackRequest);
            SupportedEndpoints["v1"].Add("gates", ProcessGatesRequest);

            RequestHandlers.Add(ApiRequestType.setExternal, ProcessCommandSetExternal);
            RequestHandlers.Add(ApiRequestType.callService, ProcessCommandCallService);
            RequestHandlers.Add(ApiRequestType.cancelService, ProcessCommandCancelService);
            RequestHandlers.Add(ApiRequestType.queueService, ProcessCommandQueueService);
            RequestHandlers.Add(ApiRequestType.setPaxCount, ProcessCommandSetPaxCount);
            RequestHandlers.Add(ApiRequestType.setBagCount, ProcessCommandSetBagCount);
            RequestHandlers.Add(ApiRequestType.setPaxDiff, ProcessCommandSetPaxDiff);
            RequestHandlers.Add(ApiRequestType.setFuelPlanned, ProcessCommandSetFuelPlanned);
            RequestHandlers.Add(ApiRequestType.sendFinal, ProcessCommandSendFinal);
            RequestHandlers.Add(ApiRequestType.setSavedFob, ProcessCommandSetSavedFob);
            RequestHandlers.Add(ApiRequestType.setPushDirection, ProcessCommandSetPushDirection);
            RequestHandlers.Add(ApiRequestType.callPushback, ProcessCommandCallPushback);
            RequestHandlers.Add(ApiRequestType.setGateArrival, ProcessCommandSetGateArrival);
            RequestHandlers.Add(ApiRequestType.smartCall, ProcessCommandSmartCall);
            RequestHandlers.Add(ApiRequestType.setProfile, ProcessCommandSetProfile);
            RequestHandlers.Add(ApiRequestType.getProfile, ProcessCommandGetProfile);
        }

        protected override Task AfterStart()
        {
            UpdatePortInfo();

            return Task.CompletedTask;
        }

        public virtual void UpdatePortInfo()
        {
            try
            {
                SimStore[Definition.ApiPortLvar].WriteValue(Port);
                File.WriteAllText(Definition.ApiPortPath, Port.ToString());
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual Task ProcessExternalCommand(RequestEnvelope request)
        {
            var payload = DeserializeRequest<RequestPayload>(request);
            if (RequestHandlers.TryGetValue(payload.request, out var handler))
            {
                return handler?.Invoke(payload, request);
            }
            else
            {
                LogWarning($"with unsupported External Command '{payload.request}'", request);
                request.SetResponseCode(HttpStatusCode.BadRequest, "Unsupported Request");
            }

            return Task.CompletedTask;
        }

        protected virtual void SetWrongFormat(RequestPayload payload, RequestEnvelope request)
        {
            LogWarning($"with unsupported Format for '{payload.request}'", request);
            request.SetResponseCode(HttpStatusCode.BadRequest, "Wrong Message Format");
        }

        protected virtual Task ProcessCommandSetExternal(RequestPayload payload, RequestEnvelope request)
        {
            if (payload.TryGetData<bool>(out bool state))
            {
                if (!string.IsNullOrWhiteSpace(payload.handler))
                    AutomationController.DepartureQueue.SetExternalControl(state, payload.handler);
                else
                    AutomationController.DepartureQueue.SetExternalControl(state);

                request.SetOk();
            }
            else
                SetWrongFormat(payload, request);

            return Task.CompletedTask;
        }

        protected virtual Task ProcessCommandCallService(RequestPayload payload, RequestEnvelope request)
        {
            var data = payload.DeserializeData<PayloadService>();

            if (data.service != GsxServiceType.Unknown)
            {
                Logger.Information($"External Control: Service Call Request for {data.service}");
                bool queued = AutomationController.DepartureQueue.HasCalled(s => s.Service.Type == data.service) || AutomationController.DepartureQueue.HasQueued(s => s.Service.Type == data.service);

                if (!queued && AutomationController.DepartureQueue.QueueExternalService(new(data.service, GsxServiceActivation.Independent, TimeSpan.Zero, GsxServiceDependency.Requested, GsxServiceDependencyScope.LastCalled, false, false, false, false, true, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero)))
                    request.SetOk();
                else
                {
                    Logger.Warning($"External Control: Service could not be queued");
                    request.SetResponseCode(HttpStatusCode.BadRequest, $"Service {data.service} could not be queued");
                }
            }
            else
                SetWrongFormat(payload, request);

            return Task.CompletedTask;
        }

        protected virtual async Task ProcessCommandCancelService(RequestPayload payload, RequestEnvelope request)
        {
            var data = payload.DeserializeData<PayloadService>();

            if (data.service != GsxServiceType.Unknown)
            {
                Logger.Information($"External Control: Service Cancel Request for {data.service}");
                if ((GsxController.GetService(data.service).IsCompleted || GsxController.GetService(data.service).IsCompleting) && await AutomationController.DepartureQueue.CancelService(data.service, "External Request"))
                    request.SetOk();
                else
                {
                    Logger.Warning($"External Control: Service failed to cancel");
                    request.SetResponseCode(HttpStatusCode.InternalServerError, $"Service {data.service} failed to cancel");
                }
            }
            else
                SetWrongFormat(payload, request);
        }

        protected virtual Task ProcessCommandQueueService(RequestPayload payload, RequestEnvelope request)
        {
            var data = payload.DeserializeData<PayloadQueueService>();

            if (data.service != GsxServiceType.Unknown)
            {
                bool queued = AutomationController.DepartureQueue.HasCalled(s => s.Service.Type == data.service) || AutomationController.DepartureQueue.HasQueued(s => s.Service.Type == data.service);
                var service = new ServiceConfig(data.service, data.activation ?? GsxServiceActivation.ServiceDependent, data.timeDeparture ?? TimeSpan.Zero, data.serviceDependency ?? GsxServiceDependency.Requested, data.dependencyScope ?? GsxServiceDependencyScope.LastCalled,
                                                false, false, false, false, true, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero);
                Logger.Information($"External Control: Service Queue Request for {data.service} (Activation: {service.ServiceActivation} | TimeDeparture: {service.MaxTimeBeforeDeparture} | ServiceDependency {service.ServiceDependency} | DependencyScope {service.ServiceDependencyScope})");
                if (!queued && AutomationController.DepartureQueue.QueueExternalService(service))
                    request.SetOk();
                else
                {
                    Logger.Warning($"External Control: Service could not be queued");
                    request.SetResponseCode(HttpStatusCode.BadRequest, $"Service {data.service} could not be queued");
                }
            }
            else
                SetWrongFormat(payload, request);

            return Task.CompletedTask;
        }

        protected virtual Task ProcessCommandSetPaxCount(RequestPayload payload, RequestEnvelope request)
        {
            if (payload.TryGetData<int>(out int paxCount))
            {
                if (paxCount >= 0 && paxCount <= Flightplan.MaxPax)
                {
                    Logger.Information($"External Control: Planned Pax updated to {paxCount}");
                    Flightplan.UpdatePassengerCount(paxCount, Flightplan.DiffPax);
                    Flightplan.UpdateBagCount(paxCount, Flightplan.DiffPax);
                    request.SetOk();
                }
                else
                {
                    Logger.Warning($"External Control: Planned Pax could not be set");
                    request.SetResponseCode(HttpStatusCode.BadRequest, $"Planned Count was negative or above max");
                }
            }
            else
                SetWrongFormat(payload, request);

            return Task.CompletedTask;
        }

        protected virtual Task ProcessCommandSetBagCount(RequestPayload payload, RequestEnvelope request)
        {
            if (payload.TryGetData<int>(out int bagCount))
            {
                if (bagCount >= 0 && bagCount <= Flightplan.MaxPax)
                {
                    Logger.Information($"External Control: Planned Bags updated to {bagCount}");
                    Flightplan.UpdateBagCount(bagCount, Flightplan.DiffPax);
                    request.SetOk();
                }
                else
                {
                    Logger.Warning($"External Control: Planned Bags could not be set");
                    request.SetResponseCode(HttpStatusCode.BadRequest, $"Planned Count was negative or above max");
                }
            }
            else
                SetWrongFormat(payload, request);

            return Task.CompletedTask;
        }

        protected virtual Task ProcessCommandSetPaxDiff(RequestPayload payload, RequestEnvelope request)
        {
            if (payload.TryGetData<int>(out int diff))
            {
                Logger.Information($"External Control: Pax/Bag Difference updated to {diff}");
                Flightplan.UpdatePassengerCount(Flightplan.CountPaxPlanned, diff);
                Flightplan.UpdateBagCount(Flightplan.CountBagsPlanned, diff);
                request.SetOk();
            }
            else
                SetWrongFormat(payload, request);

            return Task.CompletedTask;
        }

        protected virtual Task ProcessCommandSetFuelPlanned(RequestPayload payload, RequestEnvelope request)
        {
            if (payload.TryGetData<int>(out int fuel))
            {
                if (fuel >= 0 && fuel <= AircraftController.Aircraft?.FuelCapacityKg)
                {
                    Logger.Information($"External Control: Planned Fuel updated to {fuel}");
                    Flightplan.UpdatePlannedFuelKg(fuel);
                    request.SetOk();
                }
                else
                {
                    Logger.Warning($"External Control: Planned Fuel could not be set");
                    request.SetResponseCode(HttpStatusCode.BadRequest, $"Planned Fuel was negative or above max");
                }
            }
            else
                SetWrongFormat(payload, request);

            return Task.CompletedTask;
        }

        protected virtual Task ProcessCommandSendFinal(RequestPayload payload, RequestEnvelope request)
        {
            Logger.Information($"External Control: Loadsheet sent");
            AutomationController.SendExternalLoadsheet();
            request.SetOk();

            return Task.CompletedTask;
        }

        protected virtual async Task ProcessCommandSetSavedFob(RequestPayload payload, RequestEnvelope request)
        {
            var data = payload.DeserializeData<PayloadSetFob>();
            if (!string.IsNullOrWhiteSpace(data.title) && data.fuel > 0)
            {
                int fuel = data.fuel ?? 2500;
                Config.SetFuelFob(data.title, fuel);
                Logger.Information($"External Control: Saved FOB updated for '{data.title}' to {fuel}{Config.DisplayUnitCurrentString}");
                if (data.set == true)
                {
                    Logger.Information($"External Control: Aircraft FOB set to {fuel}{Config.DisplayUnitCurrentString}");
                    await AircraftController.Aircraft.SetFuelOnBoardKg(fuel, Flightplan?.FuelRampKg ?? 0);
                }
                request.SetOk();
            }
            else
                SetWrongFormat(payload, request);
        }

        protected virtual Task ProcessCommandSetPushDirection(RequestPayload payload, RequestEnvelope request)
        {
            if (payload.TryGetData<string>(out string label))
            {
                if (!string.IsNullOrWhiteSpace(label))
                    Logger.Information($"External Control: Push Direction received - Label '{label}'");
                else
                    Logger.Information($"External Control: Push Direction cleared");

                GsxController.SetPushDirection(label);
                request.SetOk();
            }
            else
                SetWrongFormat(payload, request);

            return Task.CompletedTask;
        }

        protected virtual async Task ProcessCommandCallPushback(RequestPayload payload, RequestEnvelope request)
        {
            payload.TryGetData<string>(out string label);
            Logger.Information($"External Control: Push Request received{(!string.IsNullOrWhiteSpace(label) ? " - Label '" + label + "'" : "")}");

            if (!string.IsNullOrWhiteSpace(label))
                GsxController.SetPushDirection(label);
            await GsxController.ServicePushBack.Call();

            request.SetOk();
        }

        protected virtual Task ProcessCommandSetGateArrival(RequestPayload payload, RequestEnvelope request)
        {
            if (payload.HasData && (payload.data.GetValueKind() == JsonValueKind.String || payload.data.GetValueKind() == JsonValueKind.Number))
            {
                GsxController.SetArrivalGate(payload.data);
                Logger.Information($"External Control: Arrival Gate received - Name '{GsxController.AutoSelectionArrivalGate}'");
                request.SetOk();
            }
            else
                SetWrongFormat(payload, request);

            return Task.CompletedTask;
        }

        protected virtual Task ProcessCommandSmartCall(RequestPayload payload, RequestEnvelope request)
        {
            Logger.Information($"External Control: SmartButton Request received");
            AutomationController.HasExternalSmartButtonRequest = true;
            request.SetOk();

            return Task.CompletedTask;
        }

        protected virtual Task ProcessCommandSetProfile(RequestPayload payload, RequestEnvelope request)
        {
            if (payload.TryGetDataKey<string>("key", out string key) && !string.IsNullOrWhiteSpace(key) && payload.HasDataKey("value"))
            {
                bool result = false;
                var value = payload.data!?["value"];
                try
                {
                    var profile = AppService.SettingProfile;
                    if (profile.HasProperty(key))
                    {
                        bool changed = false;
                        if (value?.GetValueKind() == JsonValueKind.Number && profile.IsPropertyType<double>(key))
                        {
                            profile.SetPropertyValue<double>(key, value.GetValue<double>());
                            changed = true;
                        }
                        else if (value?.GetValueKind() == JsonValueKind.Number && profile.IsPropertyType<int>(key))
                        {
                            profile.SetPropertyValue<int>(key, value.GetValue<int>());
                            changed = true;
                        }
                        else if ((value?.GetValueKind() == JsonValueKind.True || value?.GetValueKind() == JsonValueKind.False) && profile.IsPropertyType<bool>(key))
                        {
                            profile.SetPropertyValue<bool>(key, value.GetValue<bool>());
                            changed = true;
                        }
                        else if (value?.GetValueKind() == JsonValueKind.String && profile.IsPropertyType<string>(key))
                        {
                            profile.SetPropertyValue<string>(key, value.GetValue<string>());
                            changed = true;
                        }

                        if (changed)
                        {
                            Config.SaveConfiguration();
                            result = true;
                        }
                    }
                }
                catch
                {
                    result = false;
                }

                if (result)
                {
                    Logger.Information($"External Control: Profile Setting '{key}' set to '{value?.ToString()}'");
                    request.SetOk();
                }
                else
                    request.SetResponseCode(HttpStatusCode.BadRequest, $"Setting '{key}' could not be set");
            }
            else
                SetWrongFormat(payload, request);

            return Task.CompletedTask;
        }

        protected virtual Task ProcessCommandGetProfile(RequestPayload payload, RequestEnvelope request)
        {
            if (payload.TryGetDataKey<string>("key", out string key) && !string.IsNullOrWhiteSpace(key))
            {
                if (AppService.SettingProfile.HasProperty(key))
                {
                    var response = new Dictionary<string, object>()
                    {
                        { key, AppService.SettingProfile.GetPropertyValue<object>(key) },
                    };
                    request.SetResonseContent(response);
                }
                else
                    request.SetResponseCode(HttpStatusCode.NotFound, $"Setting '{key}' could not be found");
            }
            else
                SetWrongFormat(payload, request);

            return Task.CompletedTask;
        }

        protected virtual Task ProcessStatusRequest(RequestEnvelope request)
        {
            request.SetResonseContent(new ResponseStatus(this));

            return Task.CompletedTask;
        }

        protected virtual Task ProcessPushbackRequest(RequestEnvelope request)
        {
            request.SetResonseContent(new ResponsePushback(this));

            return Task.CompletedTask;
        }

        protected virtual Task ProcessGatesRequest(RequestEnvelope request)
        {
            request.SetResonseContent(new ResponseGates(this));

            return Task.CompletedTask;
        }
    }
}
