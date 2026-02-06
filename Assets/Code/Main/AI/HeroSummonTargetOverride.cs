using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.AI {
    public partial class HeroSummonTargetOverride : TargetOverrideElement {
        readonly NpcHeroSummon _summon;
        
        public override ICharacter Target => _active && InRange ? _target : null;
        bool InRange => _target != null && _summon.InTripledAllyRange(_target.Coords);
        
        public static void AddSummonTargetOverrideElement(NpcHeroSummon summon, ICharacter target, int priority, Status status = null) {
            summon.ParentModel.RemoveMarkerElement<HeroSummonTargetOverride>();
            summon.ParentModel.AddMarkerElement(() => new HeroSummonTargetOverride(summon, target, priority, status));
        }
        
        HeroSummonTargetOverride(NpcHeroSummon summon, ICharacter target, int priority, Status status = null) : base(target, priority, status) {
            _summon = summon;
        }

        protected override void OnInitialize() {
            if (_summon.ParentModel is { HasCompletelyInitialized: true }) {
                _summon.EnterCombat();
            }
            base.OnInitialize();
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            bool npcIsDiscarded = _summon.ParentModel?.HasBeenDiscarded ?? true;
            if (!fromDomainDrop && !npcIsDiscarded) {
                _summon.TryExitCombat();
            }
        }
    }
}