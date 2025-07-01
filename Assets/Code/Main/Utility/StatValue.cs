using System;
using System.Globalization;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Utility.Skills;
using UnityEngine;

namespace Awaken.TG.Main.Utility {
    [Serializable]
    public struct StatValue {
        
        // === Fields & Properties
        [SerializeField] public float value;
        [SerializeField] ValueType type;

        public ValueType ValueType => type;
        
        // === Constructors

        public StatValue(float value, ValueType type = ValueType.Flat) {
            this.value = value;
            this.type = type;
        }

        // === Public API
        public static StatValue Percent(StatValue statValue) {
            statValue.type = ValueType.Percent;
            return statValue;
        }

        public float GetValue(Stat stat) => SkillsUtils.StatValueToValue(stat, value, ValueType);

        public string Label() {
            return $"{value.ToString(CultureInfo.InvariantCulture)} {ValueType.ToString()}";
        }
    }
    
    public enum ValueType {
        Flat,
        Percent,
        PercentOfMax,
    }
}

