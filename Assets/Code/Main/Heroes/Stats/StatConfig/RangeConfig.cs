using System;
using Awaken.TG.Main.General;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Stats.StatConfig {
    [Serializable]
    public class RangeConfig {
        [HorizontalGroup("Range", 90f), HideLabel]
        public StatDefinedRange rangeType = StatDefinedRange.Tiny;
        
        [SerializeField, HorizontalGroup("Range"), HideIf(nameof(useConst)), HideLabel]
        FloatRange range;

        [SerializeField, HorizontalGroup("Range"), ShowIf(nameof(useConst)), HideLabel]
        float constValue;
        
        [SerializeField, HorizontalGroup("Range"), LabelText("Const"), LabelWidth(70)]
        bool useConst;

        public FloatRange Range => useConst ? new FloatRange(constValue, constValue) : range;
        public float RandomValue => Range.RandomPick();
    }
}