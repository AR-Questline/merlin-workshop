using System;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Utility.RichEnums;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Awaken.TG.Main.Wyrdnessing.WyrdEmpowering {
    [Serializable]
    public class WyrdEmpoweredStat {
        [SerializeField, RichEnumExtends(typeof(StatType))] 
        RichEnumReference statEffected;
        [SerializeField, RichEnumExtends(typeof(OperationType))]
        RichEnumReference effectType;

        [SerializeField] bool isRandomized;

        [ShowIf(nameof(isRandomized)), SerializeField, HorizontalGroup]
        float min;

        [ShowIf(nameof(isRandomized)), SerializeField, HorizontalGroup]
        float max;
        
        [HideIf(nameof(isRandomized)),SerializeField]
        float effectStrength;
        
        public StatType StatEffected => statEffected.EnumAs<StatType>();
        public OperationType EffectType => effectType.EnumAs<OperationType>();

        public float GetStrength() {
            return isRandomized ? Random.Range(min, max) : effectStrength;
        }
    }
}
