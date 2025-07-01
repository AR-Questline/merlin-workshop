using System;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.Main.VisualGraphUtils;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Generated.Stats {
    [UnitCategory("AR/Stats")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetStat : ARUnit {
        protected override void Definition() {
            var statTypeInput = RequiredARValueInput<StatType>("statType");
            var iWithStatsInput = FallbackARValueInput("iWithStat", VGUtils.My<IWithStats>);
            ValueOutput("stat", flow => {
                StatType statType = statTypeInput.Value(flow);
                if (statType == null) {
                    throw new NullReferenceException("Null stat type");
                }

                IWithStats withStats = iWithStatsInput.Value(flow);
                Stat stat = withStats.Stat(statType);
                
                if (stat == null && withStats is Hero or Item) {
                    throw new NullReferenceException($"Stat {statType.EnumName} not found in {LogUtils.GetDebugName(withStats)}");
                }
                return stat;
            });
        }
    }
}