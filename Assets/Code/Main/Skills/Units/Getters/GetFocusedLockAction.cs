using System.Linq;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Getters {
    [UnitCategory("AR/Skills/Getters")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetFocusedLockAction : ARUnit {
        protected override void Definition() {
            ValueOutput<bool>("isLookingAtLock", _ => {
                LockAction firstOrDefault = GetLookedAtLockedLock();
                return firstOrDefault != null;
            });
            
            ValueOutput<LockAction>("lock", _ => GetLookedAtLockedLock());
        }

        static LockAction GetLookedAtLockedLock() {
            var caster = Hero.Current.VHeroController.Raycaster;
            LockAction firstOrDefault = caster.GetAvailableActions().FirstOrDefault(a => a is LockAction {Locked: true}) as LockAction;
            return firstOrDefault;
        }
    }
}