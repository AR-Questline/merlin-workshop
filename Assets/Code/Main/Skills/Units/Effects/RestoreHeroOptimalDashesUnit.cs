using Awaken.TG.Main.Heroes;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [UnityEngine.Scripting.Preserve]
    public class RestoreHeroOptimalDashesUnit : ARUnit {
        protected override void Definition() {
            DefineSimpleAction(_ => {
                Hero.Current.HeroDash.ApplyPersistentOptimalStatus();
            });
        }
    }
}