using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Stories;

namespace Awaken.TG.Main.Fights.Duels {
    public partial class DefeatedHeroDuelistElement : HeroInvolvement<HeroDuelistElement> {
        public sealed override bool IsNotSaved => true;

        DefeatedInDuelInvisibility _defeatedInvisibility;
        
        public override Location FocusedLocation => GetLastLivingNpc();
        
        protected override void OnInitialize() {
            base.OnInitialize();
            Hero.VHeroController?.StoryBasedCrouch(true);
            _defeatedInvisibility = Hero.AddElement<DefeatedInDuelInvisibility>();
        }
        
        Location GetLastLivingNpc() {
            var hostileTarget = ParentModel.DuelController.GetHostileTarget(ParentModel.GroupId);
            if (hostileTarget is { ParentModel: { HasBeenDiscarded: false } and NpcElement npc }) {
                return npc.ParentModel;
            }
            return null;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            base.OnDiscard(fromDomainDrop);
            if (fromDomainDrop) {
                return;
            }
            Hero?.VHeroController?.StoryBasedCrouch(false);
            if (_defeatedInvisibility is { HasBeenDiscarded: false }) {
                _defeatedInvisibility.Discard();
            }
        }
    }
}
