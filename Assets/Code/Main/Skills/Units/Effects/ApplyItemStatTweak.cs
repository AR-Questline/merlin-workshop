using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills")]
    [UnityEngine.Scripting.Preserve]
    public class ApplyItemStatTweak : ARUnit, ISkillUnit {
        ControlOutput _exit;
        
        protected override void Definition() {
            _exit = ControlOutput("exit");
            var item = ValueInput(typeof(IWithStats), "item");
            var dictionary = FallbackARValueInput<AotDictionary>("dictionary", _ => null);
            var statTypeInput = ValueInput(typeof(ItemStatType), "statType");
            var modifierInput = ValueInput(typeof(float), "modifier");
            var operationTypeInput = FallbackARValueInput("operationType", _ => OperationType.Add);
            var tweakedStat = ValueOutput(typeof(StatTweak), "tweakedStat");

            var enter = ControlInput("enter", flow => {
                var owner = item.GetValue<IWithStats>(flow, _ => null);
                var skill = this.Skill(flow);
                var statType = statTypeInput.GetValue<ItemStatType>(flow, _ => null);
                var modifier = modifierInput.GetValue<float>(flow, _ => 0);
                var operationType = operationTypeInput.Value(flow);
                StatTweak statTweak = ApplyStatTweak(owner, statType, modifier, operationType, skill);
                flow.SetValue(tweakedStat, statTweak);

                var aotDictionary = dictionary.Value(flow);
                if (aotDictionary != null) {
                    aotDictionary[owner] = statTweak;
                }
                return statTweak == null ? null : _exit;
            });
            Requirement(item, tweakedStat);
            Requirement(statTypeInput, tweakedStat);
            Requirement(modifierInput, tweakedStat);
            Succession(enter, _exit);
        }

        static StatTweak ApplyStatTweak(IWithStats iWithStats, StatType statType, float modifier, OperationType operationType, Model statTweakOwner) {
            if (iWithStats == null) {
                Log.Important?.Error("iWithStats is null for graph: " + LogUtils.GetDebugName(statTweakOwner));
                return null;
            }
            if (statType == null) {
                Log.Important?.Error("statType is null for graph: " + LogUtils.GetDebugName(statTweakOwner));
                return null;
            }
            Stat tweakedStat = iWithStats.Stat(statType);
            if (tweakedStat == null) {
                Log.Important?.Error($"{LogUtils.GetDebugName(iWithStats)} does not have a stat of type {statType.EnumName}");
                return null;
            }
            StatTweak statTweak = new StatTweak(tweakedStat, modifier, null, operationType, statTweakOwner);
            return statTweak;
        }
    }
}