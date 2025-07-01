using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Idle.Data.Runtime;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Elements;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Fights.Duels {
    public partial class DefeatedNpcDuelistElement : Element<NpcDuelistElement> {
        public sealed override bool IsNotSaved => true;

        InteractionOverride _defeatedInteractionOverride;
        DefeatedInDuelInvisibility _defeatedInvisibility;

        NpcElement Npc => ParentModel.ParentModel as NpcElement;
        ARAssetReference Animations => ParentModel.Settings.defeatedAnimationsOverrides is { IsSet: true } overrides ? 
                                                            overrides : GameConstants.Get.defeatedDuelistAnimations;
        
        protected override void OnInitialize() {
            if (Npc.IsSummon) {
                KillDelay().Forget();
                return;
            }
            StartDefeatInteraction(Npc);
            _defeatedInvisibility = Npc.AddElement<DefeatedInDuelInvisibility>();
        }

        async UniTaskVoid KillDelay() {
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }
            Npc.ParentModel.Kill();
        }
        
        void StartDefeatInteraction(NpcElement npc) {
            npc.NpcAI.ForceStopCombatWithHero();
            npc.Interactor.Stop(InteractionStopReason.StoppedIdlingInstant, false);
            bool canBeTalkedTo = ParentModel.Settings.canBeTalkedToDefeated;
            npc.Behaviours.AddOverride(new InteractionDefeatedDuelistFinder(npc.Hips.position, Animations, canBeTalkedTo), null);
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            if (_defeatedInteractionOverride is { HasBeenDiscarded: false} ) {
                _defeatedInteractionOverride.Discard();
            }

            if (_defeatedInvisibility is { HasBeenDiscarded: false }) {
                _defeatedInvisibility.Discard();
            }
        }
    }
}
