using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Pattern {
    public partial class TalentTreePatternElement : Element<ITreePatternHost> {
        public sealed override bool IsNotSaved => true;
        VTalentTreePatternBase _viewPrefab;
        
        public TalentTreePatternElement(TalentTreeTemplate treeTemplate) {
            _viewPrefab = treeTemplate.Pattern;
        }
        
        protected override void OnFullyInitialized() {
            World.SpawnViewFromPrefab<VTalentTreePattern>(this, _viewPrefab.gameObject, true, true, ParentModel.TreeParent);
        }
    }
}
