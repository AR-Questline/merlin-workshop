using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Heroes.Stats {
    [Serializable]
    public partial class Stat {
        public virtual ushort TypeForSerialization => SavedTypes.Stat;

        // === Properties

        [Saved] public IWithStats Owner { get; private set; }
        [Saved] public virtual StatType Type { get; private set; }
        [Saved] public virtual float BaseValue { get; private set; }
        [Saved] float _previousValue;

        public virtual float ModifiedValue => CachedModifiedValue ?? BaseValue;
        public int BaseInt => Mathf.CeilToInt(BaseValue);

        public int ModifiedInt => Mathf.CeilToInt(ModifiedValue);
        
        public virtual float ValueForSave => BaseValue;

        public float PredictedModification {
            get {
                if (_modifications == null) {
                    return 0;
                }
                float sum = 0;
                var values = _modifications.Values;
                foreach (var value in values) {
                    sum += value;
                }

                return sum;
            }
        }
        Dictionary<IModel, float> _modifications;

        [UnityEngine.Scripting.Preserve] public float PredictedValue => ModifiedValue + PredictedModification;
        protected virtual float? CachedModifiedValue { get; set; }
        

        // === Events

        public struct StatChange {
            public Stat stat;
            public ContractContext context;
            public float value;
            [UnityEngine.Scripting.Preserve] public readonly float originalValue;

            public StatChange(Stat stat, float value, ContractContext context = null) {
                this.stat = stat;
                this.value = originalValue = value;
                this.context = context;
            }
        }

        [Il2CppEagerStaticClassConstruction]
        public static class Events {
            static readonly OnDemandCache<StatType, Event<IWithStats, Stat>> StatChangedCache = new(st => new($"StatChanged/{st.Serialize()}"));
            static readonly OnDemandCache<StatType, Event<IWithStats, StatChange>> StatChangedByCache = new(st => new($"StatChangedBy/{st.Serialize()}"));
            static readonly OnDemandCache<StatType, HookableEvent<IWithStats, StatChange>> ChangingStatCache = new(st => new($"ChangingStat/{st.Serialize()}"));

            public static Event<IWithStats, Stat> StatChanged(StatType statType) => StatChangedCache[statType];
            public static readonly Event<IWithStats, Stat> AnyStatChanged = new(nameof(AnyStatChanged));

            public static Event<IWithStats, StatChange> StatChangedBy(StatType statType) => StatChangedByCache[statType];
            public static readonly Event<IWithStats, StatChange> AnyStatChangedBy = new(nameof(AnyStatChangedBy));
            public static HookableEvent<IWithStats, StatChange> ChangingStat(StatType statType) => ChangingStatCache[statType];
        }

        // === Constructions
        
        public Stat(IWithStats owner, StatType type, float initialValue) {
            Owner = owner;
            Type = type;
            BaseValue = _previousValue = initialValue;
        }

        protected Stat() { } // serialization only

        // === Calculation of modified value
        
        public void WriteSavables(JsonWriter writer, JsonSerializer serializer) {
            writer.WriteStartObject();
            
            JsonUtils.JsonWrite(writer, serializer, nameof(Owner), Owner);
            JsonUtils.JsonWrite(writer, serializer, nameof(Type), Type);
            JsonUtils.JsonWrite(writer, serializer, nameof(BaseValue), BaseValue);
            JsonUtils.JsonWrite(writer, serializer, nameof(_previousValue), _previousValue);
            WriteAdditionalSavables(writer, serializer);

            writer.WriteEndObject();
        }

        protected virtual void WriteAdditionalSavables(JsonWriter writer, JsonSerializer serializer) {}

        public float RecalculateTweaks(bool triggerOwner = true, ContractContext context = null) {
            float newValue = World.Services.Get<TweakSystem>().Recalculate(this);
            CachedModifiedValue = newValue;
            
            if (triggerOwner) {
                CallStatChangedEvents();
                if (!Mathf.Approximately(_previousValue, newValue)) {
                    StatChange change = new(this, newValue - _previousValue, context);
                    Owner.Trigger(Events.StatChangedBy(Type), change);
                    Owner.Trigger(Events.AnyStatChangedBy, change);
                    StatType.Events.TriggerStatOfTypeChanged(Owner, this);
                }
            }

            _previousValue = newValue;
            return CachedModifiedValue.Value;
        }

        // === Operations on value

        public virtual bool SetTo(float newValue, bool runHooks = true, ContractContext context = null) {
            if (runHooks) {
                newValue = RunHooks(newValue, context, out bool prevented);
                if (prevented) {
                    return false;
                }
            }

            if (float.IsNaN(newValue)) {
                Log.Important?.Error($"Trying to set NaN value to stat {Type.EnumName} {Type.DisplayName} {LogUtils.GetDebugName(Owner)}");
                return false;
            }

            InternalSetTo(newValue, context);
            return true;
        }

        /// <summary>
        /// So, if value is less then <paramref name="newValue"/> then value will be equal to <paramref name="newValue"/>.
        /// Otherwise current value will be increased by <paramref name="delta"/>.<br/>
        /// It will always call the events for stat change
        /// </summary>
        /// <param name="newValue">Minimum value to be set</param>
        /// <param name="delta">How much stat will change if above <paramref name="newValue"/></param>
        public virtual bool SetAtLeastTo(float newValue, float delta = M.Epsilon, bool runHooks = true, ContractContext context = null) {
            return newValue > BaseValue ? SetTo(newValue, runHooks, context) : IncreaseBy(delta, context);
        }

        protected virtual float RunHooks(float newValue, ContractContext context, out bool prevented) {
            var statChange = new StatChange(this, newValue - BaseValue, context);
            var hookResult = Events.ChangingStat(Type).RunHooks(Owner, statChange);
            prevented = hookResult.Prevented;
            return BaseValue + hookResult.Value.value;
        }

        protected virtual void InternalSetTo(float newValue, ContractContext context) {
            BaseValue = newValue;
            RecalculateTweaks(context: context);
        }

        public virtual bool IncreaseBy(float amount, ContractContext context = null) => SetTo(BaseValue + amount, true, context);

        public virtual bool DecreaseBy(float amount, ContractContext context = null) => SetTo(BaseValue - amount, true, context);

        public float GetPrediction(IModel predictor) {
            return _modifications?.GetValueOrDefault(predictor, 0) ?? 0;
        }

        public void SetPrediction(IModel owner, float value) {
            _modifications ??= new Dictionary<IModel, float>();
            if (_modifications.TryAdd(owner, value)) {
                owner.ListenTo(Model.Events.BeforeDiscarded, RemovePredictor, owner);
            } else {
                _modifications[owner] = value;
            }
            CallStatChangedEvents();
        }

        public void RemovePredictor(IModel predictor) {
            if (_modifications == null) {
                return;
            }
            if (_modifications.Remove(predictor)) {
                CallStatChangedEvents();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TriggerStatChanged() {
            Owner.Trigger(Events.StatChanged(Type), this);
            Owner.Trigger(Events.AnyStatChanged, this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CallStatChangedEvents() {
            if (Owner.WasDiscardedFromDomainDrop) return;
            
            Owner.TriggerChange();
            TriggerStatChanged();
        }

        // === Conversions

        public static implicit operator float(Stat stat) => stat?.ModifiedValue ?? 0f;
        public static explicit operator int(Stat stat) => stat?.ModifiedInt ?? 0;

        public override string ToString() {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            // if no modifiers are applied, this WILL be the exact same value
            if (ModifiedValue == BaseValue) {
                return $"{ModifiedValue} {Type.IconTag}";
            } else {
                return $"{ModifiedValue}({BaseValue}) {Type.IconTag}";
            }
        }

        public static float ToMultiplier(float? stat) => stat == null ? 1 : (100f + stat.Value) / 100;
    }
}