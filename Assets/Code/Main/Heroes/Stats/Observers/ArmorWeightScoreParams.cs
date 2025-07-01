using System;
using System.Linq;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility.RichEnums;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Stats.Observers {
    [Serializable]
    public struct ArmorWeightScoreParams {
        [SerializeField, RichEnumExtends(typeof(StatType))] 
        RichEnumReference stat;

        [SerializeField, RichEnumExtends(typeof(OperationType))] 
        RichEnumReference operation;

        [SerializeField] WeightValue[] values;

        public StatType StatType => stat.EnumAs<StatType>();
        public OperationType OperationType => operation.EnumAs<OperationType>();
        
        public readonly float GetValue(ItemWeight itemWeight) => values.FirstOrDefault(v => v.ItemWeight == itemWeight).Value;
        public readonly bool IsPenalty(ItemWeight itemWeight) => values.FirstOrDefault(v => v.ItemWeight == itemWeight).IsPenalty;

        [Serializable]
        struct WeightValue {
            [SerializeField, RichEnumExtends(typeof(ItemWeight))] 
            RichEnumReference weight;

            [SerializeField] float value;
            [SerializeField] bool isPenalty;

            public ItemWeight ItemWeight => weight.EnumAs<ItemWeight>();
            public float Value => value;
            public bool IsPenalty => isPenalty;
        }
    }
}