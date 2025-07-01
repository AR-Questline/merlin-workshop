using Awaken.Utility;
using System;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Saving;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Stats
{
    /// <summary>
    /// This type of stat is calculated from two other stats, returning random value between given stat values.
    /// </summary>
    public sealed partial class RandomRangeStat : Stat {
        public override ushort TypeForSerialization => SavedTypes.RandomRangeStat;

        // === Private types

        [Serializable]
        public partial struct RandomRangeStatLimit {
            public ushort TypeForSerialization => SavedTypes.RandomRangeStatLimit;

            [Saved] public float constValue;
            [Saved] public StatType statReference;
            
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

        [Saved] RandomRangeStatLimit _lowerLimit, _upperLimit;

        // === Properties

        public float LowerLimit => LimitValue(_lowerLimit);
        public float UpperLimit => LimitValue(_upperLimit);

        public float Percentage => (ModifiedValue - LowerLimit) / (UpperLimit - LowerLimit);

        [UnityEngine.Scripting.Preserve] public bool IsMax => Percentage >= 0.999999f;
        [UnityEngine.Scripting.Preserve] public bool IsMin => Percentage <= 0.000001f;

        // === Construction

        public RandomRangeStat(IWithStats owner, StatType type, float lowerLimit, float upperLimit) : base(owner, type, 0) {
            _lowerLimit = new RandomRangeStatLimit {constValue = lowerLimit};
            _upperLimit = new RandomRangeStatLimit {constValue = upperLimit};
        }

        public RandomRangeStat(IWithStats owner, StatType type, StatType lowerLimit, float upperLimit) : base(owner, type, 0) {
            _lowerLimit = new RandomRangeStatLimit {statReference = lowerLimit};
            _upperLimit = new RandomRangeStatLimit {constValue = upperLimit};
        }

        public RandomRangeStat(IWithStats owner, StatType type, float lowerLimit, StatType upperLimit) : base(owner, type, 0) {
            _lowerLimit = new RandomRangeStatLimit {constValue = lowerLimit};
            _upperLimit = new RandomRangeStatLimit {statReference = upperLimit};
        }

        public RandomRangeStat(IWithStats owner, StatType type, StatType lowerLimit, StatType upperLimit) : base(owner, type, 0) {
            _lowerLimit = new RandomRangeStatLimit {statReference = lowerLimit};
            _upperLimit = new RandomRangeStatLimit {statReference = upperLimit};
        }

        [UnityEngine.Scripting.Preserve]
        RandomRangeStat() { } // serialization only
        
        protected override void WriteAdditionalSavables(JsonWriter writer, JsonSerializer serializer) {
            writer.WritePropertyName(nameof(_lowerLimit));
            _lowerLimit.WriteSavables(writer, serializer);
            writer.WritePropertyName(nameof(_upperLimit));
            _upperLimit.WriteSavables(writer, serializer);
        }

        // === New logic 

        [JsonIgnore] public override float BaseValue {
            get {
                if (Rigged == RiggedResults.Highest) {
                    return UpperLimit;
                } else if (Rigged == RiggedResults.Lowest) {
                    return LowerLimit;
                } else {
                    return new FloatRange(LowerLimit, UpperLimit).RandomPick();
                }
            }
        }

        [JsonIgnore] public override float ModifiedValue => BaseValue;
        
        /// <summary>
        /// Rigged property can be used to force minimum or maximum results.
        /// </summary>
        [JsonIgnore] public RiggedResults Rigged { get; [UnityEngine.Scripting.Preserve] set; }

        float LimitValue(RandomRangeStatLimit limit) {
            if (limit.statReference != null) {
                Stat limitStat = Owner.Stat(limit.statReference);
                return limitStat.ModifiedValue;
            } else {
                return limit.constValue;
            }
        }

        public override bool SetTo(float newValue, bool runHooks = true, ContractContext context = null) 
            => throw new InvalidOperationException("Random Range stats cannot be changed.");
    }

    public enum RiggedResults {
        [UnityEngine.Scripting.Preserve] None = 0,
        Highest = 1,
        Lowest = 2,
    }
}
