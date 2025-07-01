using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Variables {
    [UnitCategory("AR/Skills/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SkillAssetReference : ARUnit, ISkillUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public string name;
        
        protected override void Definition() {
            ValueOutput("AssetReference", f => this.Skill(f).GetAssetReference(name));
        }
    }
    
    [UnitCategory("AR/Skills/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SkillShareableAssetReference : ARUnit, ISkillUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public string name;
        
        protected override void Definition() {
            ValueOutput("ShareableAssetReference", f => this.Skill(f).GetShareableAssetReference(name));
        }
    }
}