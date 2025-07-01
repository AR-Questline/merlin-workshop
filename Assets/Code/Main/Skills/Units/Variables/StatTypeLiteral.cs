using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Utility.RichEnums;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Variables {
    [UnitCategory("AR/Skills/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    public class StatTypeLiteral : Unit, ISkillUnit {

        [Serialize, Inspectable, UnitHeaderInspectable] 
        [RichEnumExtends(typeof(StatType))]
        public RichEnumReference reference;

        [UnityEngine.Scripting.Preserve] ValueOutput _statType;
        
        protected override void Definition() {
            _statType = ValueOutput("statType", _ => reference.EnumAs<StatType>());
        }
    }
}