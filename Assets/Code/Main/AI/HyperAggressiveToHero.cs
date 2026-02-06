using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Statuses;

namespace Awaken.TG.Main.AI {
    public class HyperAggressiveToHero : TargetOverrideElement {
        public override bool IsValid => true;
        public override bool TemporarilyDisabled => !ParentModel.PossibleTargets.Contains(_target);

        public HyperAggressiveToHero(ICharacter target, int priority, Status status = null) : base(target, priority, status) { }
        
        public static HyperAggressiveToHero Add(ICharacter character, int priority, Status status = null) {
            return character.AddElement(new HyperAggressiveToHero(Hero.Current, priority, status));
        }

        protected override void Init(NpcElement _) {
            _active = true;
        }
    }
}