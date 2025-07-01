using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.MVC.Events;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class OnMagicGauntletHitUnit : PassiveListenerWithPayloadUnit<ICharacter, MagicGauntletData> {
        protected override ICharacter Source(Skill skill, Flow flow) => skill.Owner;
        protected override Event<ICharacter, MagicGauntletData> Event(Skill skill, Flow flow) => ICharacter.Events.OnMagicGauntletHit;
    }
}