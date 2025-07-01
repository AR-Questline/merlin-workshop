using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Character;
using Awaken.TG.MVC.Events;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(FlowGraph))]
    public class BeforeAllyDeathUnit : PassiveListenerWithPayloadUnit<ICharacter, NpcAlly> {
        protected override ICharacter Source(Skill skill, Flow flow) => skill.Owner;
        protected override Event<ICharacter, NpcAlly> Event(Skill skill, Flow flow) => NpcAlly.Events.BeforeAllyDeath;
    }
}