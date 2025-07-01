using Awaken.TG.Main.Heroes.Stats;
using Awaken.Utility.UI;
using UnityEngine;

namespace Awaken.TG.Debugging.ModelsDebugs.Inspectors {
    [UnityEngine.Scripting.Preserve]
    public class StatInspector : MemberListItemInspector<Stat> {
        const int ButtonWidth = 45;
    
        public override void Draw(MembersListItem member, object value, object target, ModelsDebug modelsDebug, string[] searchContext, int searchHash) {
            if (!IsInContext(member, value, searchContext, searchHash)) {
                return;
            }
            var oldEnable = GUI.enabled;
            GUI.enabled = true;
            DrawValue(member, value, target, modelsDebug);
            GUI.enabled = oldEnable;
        }
        
        public override void DrawValue(MembersListItem member, object value, object target, ModelsDebug modelsDebug) {
            var statValue = CastedValue(value);
            RandomRangeStat randomRangeStat = statValue as RandomRangeStat;
            TGGUILayout.BeginHorizontal();
            if (randomRangeStat == null) {
                if (GUILayout.Button("-1000", GUILayout.Width(ButtonWidth))) {
                    statValue.DecreaseBy(1000);
                }
                if (GUILayout.Button("-100", GUILayout.Width(ButtonWidth))) {
                    statValue.DecreaseBy(100);
                }
                if (GUILayout.Button("-10", GUILayout.Width(ButtonWidth))) {
                    statValue.DecreaseBy(10);
                }
                if (GUILayout.Button("-1", GUILayout.Width(ButtonWidth))) {
                    statValue.DecreaseBy(1);
                }
            }
           

            string name = statValue.Type.EnumName;
            if (statValue is LimitedStat limitedStat) {
                GUILayout.Label($"[{limitedStat.LowerLimit}]{statValue.ModifiedValue}({statValue.BaseValue})[{limitedStat.UpperLimit}] - {name}");
            } else if (randomRangeStat != null) {
                GUILayout.Label($"[{randomRangeStat.LowerLimit}]{statValue.ModifiedValue}[{randomRangeStat.UpperLimit}] - {name} - {randomRangeStat.Rigged}");
            } else {
                GUILayout.Label($"{statValue.ModifiedValue}({statValue.BaseValue}) - {name}");
            }

            if (randomRangeStat == null) {
                var baseValue = statValue.BaseValue;
                baseValue = TGGUILayout.DelayedFloatField("", baseValue, GUILayout.Width(ButtonWidth * 2f));
                if (baseValue != statValue.BaseValue) {
                    statValue.SetTo(baseValue);
                }

                if (GUILayout.Button("+1", GUILayout.Width(ButtonWidth))) {
                    statValue.IncreaseBy(1);
                }

                if (GUILayout.Button("+10", GUILayout.Width(ButtonWidth))) {
                    statValue.IncreaseBy(10);
                }

                if (GUILayout.Button("+100", GUILayout.Width(ButtonWidth))) {
                    statValue.IncreaseBy(100);
                }

                if (GUILayout.Button("+1000", GUILayout.Width(ButtonWidth))) {
                    statValue.IncreaseBy(1000);
                }
            }

            TGGUILayout.EndHorizontal();
        }
    }
}