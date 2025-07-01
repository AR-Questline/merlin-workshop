using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Utility.Attributes.List;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Heroes.Stats.StatConfig {
    /// <summary>
    /// This SO defines const values for chosen stats.
    /// For example it defines what "high experience" means.
    /// </summary>
    public class StatDefinedValuesConfig : ScriptableObject {
        
        [List(ListEditOption.Buttons | ListEditOption.ElementLabels)]
        public List<StatRangeConfig> statRanges = new();
        
        StatRangeConfig Config(StatType statType) => statRanges.FirstOrDefault(sr => sr.Stat == statType);
        RangeConfig RangeConfig(StatType statType, StatDefinedRange rangeType) => Config(statType)?.ConfigFor(rangeType);
        
        // === Static access
        public static float GetValue(StatType statType, StatDefinedRange range, float customValue, float fallback = 0f) {
            if (range == StatDefinedRange.Custom) {
                return customValue;
            }

            RangeConfig config = CommonReferences.Get.StatValuesConfig.RangeConfig(statType, range);
            if (config == null) {
                Log.Important?.Warning($"Undefined Stat Range: {statType.DisplayName} - {range}");
                return fallback;
            }

            return config.RandomValue;
        }
        
        public static FloatRange? GetRange(StatType statType, StatDefinedRange range) =>
            range == StatDefinedRange.Custom ? null : CommonReferences.Get.StatValuesConfig.RangeConfig(statType, range)?.Range;
    }
}