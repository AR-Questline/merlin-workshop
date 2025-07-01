using System.Collections;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.VisualGraphUtils;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Listeners {
    [UnitCategory("AR/Skills"), UnitTitle("ForEachOwnedItem")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class ForEachPossessedItems : ARLoopUnit, ISkillUnit {
        ValueInput _character;
        
        protected override IEnumerable Collection(Flow flow) {
            ICharacter owner = _character.GetValue<ICharacter>(flow, _ => null);
            return owner.RelatedList(IItemOwner.Relations.Owns);
        }

        protected override ValueOutput Payload() => ValueOutput(typeof(Item), "item");
        
        protected override void Definition() {
            _character = ValueInput(typeof(ICharacter), "character");
            base.Definition();
        }
    }
}