using System;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Newtonsoft.Json;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Stats {
    /// <summary>
    /// A stat that has a maximum value designated by another stat. Such a stat
    /// pair forms a "bar" together, eg. current health + max health.
    /// </summary>
    public sealed partial class LimitedStat : Stat {
        public override ushort TypeForSerialization => SavedTypes.LimitedStat;

        const float Epsilon = 0.001f;
        // === Private types

        [Serializable]
        public partial class LimitedStatLimit {
            public ushort TypeForSerialization => SavedTypes.LimitedStatLimit;

            [Saved] public float constValue;
            [Saved] public StatType statReference;
            public WeakReference<Stat> statCache;

            public void WriteSavables(JsonWriter writer, JsonSerializer serializer) {
                writer.WriteStartObject();
                if (constValue != 0) {
                    JsonUtils.JsonWrite(writer, serializer, nameof(constValue), constValue);
                }
                if (statReference != null) {
                    JsonUtils.JsonWrite(writer, serializer, nameof(statReference), statReference);
                }
                writer.WriteEndObject();
            }
        }

        // === State

        [Saved(false)] public bool AllowOverflow { get; private set; }
        [Saved] LimitedStatLimit _lowerLimit, _upperLimit;

        // === Properties

        public float LowerLimit => LimitValue(_lowerLimit);
        public int LowerLimitInt => Mathf.CeilToInt(LimitValue(_lowerLimit));
        public float UpperLimit => LimitValue(_upperLimit);
        public int UpperLimitInt => Mathf.CeilToInt(LimitValue(_upperLimit));

        public float Percentage {
            get {
                if (UpperLimit == LowerLimit) {
                    return 0;
                } else {
                    return (ModifiedValue - LowerLimit) / (UpperLimit - LowerLimit);
                }
            }
        }

        public bool IsMax => ModifiedInt >= UpperLimitInt;
        [UnityEngine.Scripting.Preserve] public bool IsMin => ModifiedInt <= LowerLimitInt;
        public bool IsMaxFloat => ModifiedValue >= UpperLimit - Epsilon;
        public bool IsMinFloat => ModifiedValue <= LowerLimit + Epsilon;

        float _cachedUpper;
        float _cachedLower;
        float? _cached;
        protected override float? CachedModifiedValue {
            get {
                if (_cachedLower != LowerLimit || _cachedUpper != UpperLimit) {
                    // bounds changed it's value, we should recalculate tweaks because we might be out of bounds
                    _cachedLower = LowerLimit;
                    _cachedUpper = UpperLimit;
                    _cached = RecalculateTweaks();
                }
                return _cached;
            }
            set => _cached = value;
        }

        public override float ValueForSave => base.BaseValue;

        // === Construction

        public LimitedStat(IWithStats owner, StatType type, float initialValue, float lowerLimit, float upperLimit, bool allowOverflow = false) : base(owner, type, initialValue) {
            _lowerLimit = new LimitedStatLimit {constValue = lowerLimit};
            _upperLimit = new LimitedStatLimit {constValue = upperLimit};
            _cachedLower = LowerLimit;
            _cachedUpper = UpperLimit;
            AllowOverflow = allowOverflow;
        }

        public LimitedStat(IWithStats owner, StatType type, float initialValue, StatType lowerLimit, float upperLimit, bool allowOverflow = false) : base(owner, type, initialValue) {
            _lowerLimit = new LimitedStatLimit {statReference = lowerLimit};
            _upperLimit = new LimitedStatLimit {constValue = upperLimit};
            _cachedLower = LowerLimit;
            _cachedUpper = UpperLimit;
            AllowOverflow = allowOverflow;
        }

        public LimitedStat(IWithStats owner, StatType type, float initialValue, float lowerLimit, StatType upperLimit, bool allowOverflow = false) : base(owner, type, initialValue) {
            _lowerLimit = new LimitedStatLimit {constValue = lowerLimit};
            _upperLimit = new LimitedStatLimit {statReference = upperLimit};
            _cachedLower = LowerLimit;
            _cachedUpper = UpperLimit;
            AllowOverflow = allowOverflow;
        }

        public LimitedStat(IWithStats owner, StatType type, float initialValue, StatType lowerLimit, StatType upperLimit, bool allowOverflow = false) : base(owner, type, initialValue) {
            _lowerLimit = new LimitedStatLimit {statReference = lowerLimit};
            _upperLimit = new LimitedStatLimit {statReference = upperLimit};
            _cachedLower = LowerLimit;
            _cachedUpper = UpperLimit;
            AllowOverflow = allowOverflow;
        }

        [UnityEngine.Scripting.Preserve]
        LimitedStat() { } // serialization only

        protected override void WriteAdditionalSavables(JsonWriter writer, JsonSerializer serializer) {
            JsonUtils.JsonWrite(writer, serializer, nameof(AllowOverflow), AllowOverflow);
            writer.WritePropertyName(nameof(_lowerLimit));
            _lowerLimit.WriteSavables(writer, serializer);
            writer.WritePropertyName(nameof(_upperLimit));
            _upperLimit.WriteSavables(writer, serializer);
        }

        // === Events
        public struct LimitedStatChange {
            public readonly float valueThatWasSet;
            public readonly float desiredChangeOfValue;

            public LimitedStatChange(float valueThatWasSet, float desiredChangeOfValue) {
                this.valueThatWasSet = valueThatWasSet;
                this.desiredChangeOfValue = desiredChangeOfValue;
            }
        }

        [Il2CppEagerStaticClassConstruction]
        public new static class Events {
            static readonly OnDemandCache<StatType, Event<IWithStats, LimitedStatChange>> LimitedStatLimitsReachedCache = new(st => new($"LimitedStatChangePrevented/{st.EnumName}"));
            public static Event<IWithStats, LimitedStatChange> LimitedStatLimitsReached(StatType statType) => LimitedStatLimitsReachedCache[statType];
        }
        
        // === Operations respecting limits

        [JsonIgnore] public override float BaseValue {
            get {
                // apply limits on each get
                // the reason for that is that limiting stats might have changed in the meantime
                // this check will correct it before anybody "sees" the wrong value
                float value = base.BaseValue;
                float lower = LowerLimit, upper = UpperLimit;

                // don't allow recalculation of limited stat during loading, because some tweaks might not be loaded yet
                if (LoadingScreenUI.IsLoading) {
                    return value;
                }

                float limitedValue;
                if (AllowOverflow) {
                    limitedValue = Mathf.Max(lower, value);
                } else {
                    limitedValue = M.Mid(lower, upper, value);
                }

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                // if limits don't apply, this will be exactly == value
                if (limitedValue != value) {
                    SetTo(limitedValue, false);
                    return limitedValue;
                } else {
                    return value;
                }
            }
        }

        public override bool SetTo(float newValue, bool runHooks = true, ContractContext context = null) {
            // hooks
            if (runHooks) {
                newValue = RunHooks(newValue, context, out bool prevented);
                if (prevented) {
                    return false;
                }
            }
            // limiting to the allowed range
            float lower = LowerLimit, upper = UpperLimit;
            float beforeLimitsValue = newValue;
            newValue = AllowOverflow ? Mathf.Max(lower, newValue) : M.Mid(lower, upper, newValue);
            
            // actual set
            bool success = base.SetTo(newValue, false, context);

            if (success && beforeLimitsValue != newValue) {
                Owner.Trigger(Events.LimitedStatLimitsReached(Type), new LimitedStatChange(newValue, beforeLimitsValue));
            }
            
            return success;
        }
        
        public void SetToFull(ContractContext context = null) {
            SetTo(UpperLimit, context: context);
        }

        // === Limits
        
        public bool UpperLimitedByStat => _upperLimit.statReference != null;

        float LimitValue(LimitedStatLimit limit) {
            if (limit.statReference != null) {
                Stat limitStat = RetrieveStat(limit);
                return limitStat.ModifiedValue;
            } else {
                return limit.constValue;
            }
        }

        public float UpperBaseValue() {
            if (_upperLimit.statReference == null) {
                return _upperLimit.constValue;
            } else {
                Stat limitStat = RetrieveStat(_upperLimit);
                return limitStat.BaseValue;
            }
        }

        Stat RetrieveStat(LimitedStatLimit limit) {
            if (limit.statCache == null || !limit.statCache.TryGetTarget(out Stat stat)) {
                stat = Owner.Stat(limit.statReference);
                limit.statCache = new WeakReference<Stat>(stat);
            }

            return stat;
        }

        [UnityEngine.Scripting.Preserve]
        public void ChangeOverflow(bool enable) {
            AllowOverflow = enable;
            RecalculateTweaks();
        }

        // === ToString

        public override string ToString() {
            return $"{Type.IconTag} {ModifiedInt}/{UpperLimitInt}";
        }
    }
}
