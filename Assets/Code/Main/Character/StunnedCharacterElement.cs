using Awaken.CommonInterfaces.Animations;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Providers;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Character {
    /// <summary>
    /// Marks a character as stunned.
    /// </summary>
    public partial class StunnedCharacterElement : Element<ICharacter>, ICanMoveProvider, IAnimatorBridgeStateProvider {
        public override ushort TypeForSerialization => SavedModels.StunnedCharacterElement;

        AnimatorBridge _npcAnimator;
        public bool CanMove => false;
        public bool ForceAnimationCulling => true;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve] StunnedCharacterElement() { }
        
        public StunnedCharacterElement(Status status) {
            status.ListenTo(Events.AfterDiscarded, _ => Discard(), this);
        }

        protected override void OnInitialize() {
            if (ParentModel is NpcElement npc) {
                npc.OnCompletelyInitialized(_ => {
                    NpcCanMoveHandler.AddCanMoveProvider(npc, this);
                    _npcAnimator = AnimatorBridge.GetOrAddDefault(npc.Movement.Controller.Animator);
                    _npcAnimator.RegisterStateProvider(this);

                    npc.ListenTo(DeathElement.Events.RagdollToggled, OnRagdollToggled, this);
                    OnRagdollToggled(npc.IsInRagdoll);
                });
            }
        }

        void OnRagdollToggled(bool ragdollEnabled) {
            if (ragdollEnabled) {
                ParentModel.Element<DeathElement>().GetBehaviour<DeathRagdollBehaviour>().SetActiveRagdollConstraints(false);
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (fromDomainDrop || ParentModel is null or {HasBeenDiscarded: true}) {
                return;
            }
            
            if (_npcAnimator != null) {
                _npcAnimator.UnregisterStateProvider(this);
                _npcAnimator = null;
            }
            
            if (ParentModel is NpcElement npc) {
                NpcCanMoveHandler.RemoveCanMoveProvider(npc, this);
                if (!npc.HasBeenDiscarded && npc.IsInRagdoll) {
                    npc.Element<DeathElement>().GetBehaviour<DeathRagdollBehaviour>().SetActiveRagdollConstraints(true);
                }
            }
        }
    }
}