using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Collections.Concurrent;

namespace Any2GSX.GSX
{
    public enum StateVar
    {
        ServiceQueueInit,
        ExecutedReposition,
        WalkaroundBeforeNotified,
        WalkaroundAfterNotified,
        InitialFuelPayloadSet,
        InitialFuelPayloadQueued,
        JetwayStairRemoved,
        IsFinalReceived,
        CockpitPrepNotified,
        CockpitChocksNotified,
        TimeNextTurnCheck,
        RunDepartureOnArrival,
        SkippedToTurn,
        NoJetwayDetected,
        WasFollowMeAnswered,
        WasOperatorPreferred,
        WasStairRemoved,
        DeiceGateQuestionAnswered

    }

    public class StateStore
    {
        protected virtual ConcurrentDictionary<Enum, StateVariable> VariableStore { get; } = [];

        public dynamic this[Enum index]
        {
            get
            {
                if (VariableStore.TryGetValue(index, out StateVariable var))
                    return var.Get();
                else
                    return null;
            }

            set
            {
                if (VariableStore.TryGetValue(index, out StateVariable var) && var.Type == value.GetType())
                    var.Set(value);
            }
        }

        public virtual void Add(Enum index, object defaultValue, bool resetFlight = true, bool resetNotRunning = false)
        {
            if (VariableStore.TryGetValue(index, out StateVariable var))
                var.Set(defaultValue);
            else
                VariableStore.Add(index, new(index, defaultValue, resetFlight, resetNotRunning));
        }

        public virtual void Reset()
        {
            foreach (var item in VariableStore)
                item.Value.Reset();
        }

        public virtual void ResetNotRunning()
        {
            foreach (var item in VariableStore)
            {
                if (item.Value.ResetNonRunning)
                    item.Value.Reset();
            }
        }

        public virtual void ResetFlight()
        {
            foreach (var item in VariableStore)
            {
                if (item.Value.ResetFlight)
                    item.Value.Reset();
            }
        }

        public virtual void Clear()
        {
            foreach (var item in VariableStore)
                item.Value.Clear();

            VariableStore.Clear();
        }
    }

    public class StateVariable(Enum id, object defaultValue, bool resetFlight = true, bool resetNotRunning = false)
    {
        public virtual Enum Id { get; } = id;
        public virtual Type Type { get; } = defaultValue.GetType();
        public virtual bool ResetFlight { get; } = resetFlight;
        public virtual bool ResetNonRunning { get; } = resetNotRunning;
        protected virtual object DefaultValue { get; set; } = defaultValue;
        protected virtual object Value { get; set; } = defaultValue;

        public virtual dynamic Get()
        {
            try
            {
                return Convert.ChangeType(Value, Type);
            }
            catch
            {
                Logger.Error($"Could not convert Value for Variable '{Id}' (Type: {Type.Name})");
                return null;
            }
        }

        public virtual void Set(object value)
        {
            Value = value;
        }

        public virtual void Reset()
        {
            Value = DefaultValue;
        }

        public virtual void Clear()
        {
            DefaultValue = null;
            Value = null;
        }
    }
}
