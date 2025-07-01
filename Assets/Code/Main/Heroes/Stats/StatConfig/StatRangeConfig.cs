using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Utility.Attributes;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Utility.Attributes.List;

namespace Awaken.TG.Main.Heroes.Stats.StatConfig {
    [Serializable]
    public class StatRangeConfig {
        [RichEnumExtends(typeof(StatType))]
        [RichEnumLabel]
        public RichEnumReference statRef;

        [List(ListEditOption.Buttons)] 
        public List<RangeConfig> configs;
        
        public StatType Stat => statRef.EnumAs<StatType>();

        public RangeConfig ConfigFor(StatDefinedRange type) => configs.FirstOrDefault(c => c.rangeType == type);
    }
}