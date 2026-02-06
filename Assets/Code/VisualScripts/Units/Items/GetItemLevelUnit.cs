using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Buffs;
using Awaken.TG.Main.Heroes.Items.Gems;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Items {
    [UnitCategory("AR/Skills/Items")]
    [TypeIcon(typeof(FlowGraph))]
    [UnitTitle("Get Item Level")]
    public class GetItemLevelUnit : Unit, ISkillUnit {
        protected override void Definition() {
            ValueOutput("level", flow => Get(this, flow));
        }

        public static int Get(ISkillUnit unit, Flow flow) {
            int level = unit.Skill(flow).ParentModel switch {
                ItemEffects itemEffects => itemEffects.Item?.Level.ModifiedInt ?? 0,
                GemUnattached gemUnattached => gemUnattached.ParentModel?.Level.ModifiedInt ?? 0,
                GemAttached gemAttached => gemAttached.GemLevel,
                AppliedItemBuff appliedItemBuff => appliedItemBuff.BuffItemLevel,
#if UNITY_EDITOR
                _ => LogAndReturn($"Unsupported skill parent model type ({unit.Skill(flow)?.ParentModel?.GetType()}) for {unit.GetType()}: {LogUtils.GetDebugName(unit.Skill(flow))}", 0)
#else
                _ => 0
#endif
            };
            return level;
        }

        static int LogAndReturn(string msg, int returnValue) {
            Log.Important?.Error(msg);
            return returnValue;
        }
    }
}