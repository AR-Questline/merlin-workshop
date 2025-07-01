using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Development.WyrdPowers;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/WyrdSouls")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class UnlockWyrdSoulFragmentUnit : ARUnit {
        protected override void Definition() {
            var fragmentTypeInput = InlineARValueInput("wyrdSoulFragmentType", WyrdSoulFragmentType.Prologue);
            
            DefineSimpleAction(flow => {
                WyrdSoulFragmentType fragmentType = fragmentTypeInput.Value(flow);
                Hero.Current.Development.WyrdSoulFragments.Unlock(fragmentType);
            });
        }
    }
}