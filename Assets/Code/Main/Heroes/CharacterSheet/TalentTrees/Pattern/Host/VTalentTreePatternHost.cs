using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Pattern.Host {
    public abstract class VTalentTreePatternHost : View<ITreePatternHost> {
        [SerializeField, Required] 
        VTalentTreePatternBase pattern;

        public override Transform DetermineHost() => Target.TreeParent;

        public VTalentTreePatternBase Pattern => pattern;
    }
}
