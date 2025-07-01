using Awaken.TG.Main.Character;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class OnEnterCombatUnit : PassiveListenerWithPayloadUnit<ICharacter, ICharacter> {
        ARValueInput<ICharacter> _character;

        protected override void Definition() {
            base.Definition();
            _character = FallbackARValueInput("character", flow => this.Skill(flow)?.Owner);
        }

        protected override string DataName => "character";
        protected override ICharacter Source(Skill skill, Flow flow) => _character.Value(flow);
        protected override Event<ICharacter, ICharacter> Event(Skill skill, Flow flow) => ICharacter.Events.CombatEntered;
    }
}