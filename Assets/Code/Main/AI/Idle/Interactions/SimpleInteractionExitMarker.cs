using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    /// <summary>
    /// Marker element to determine that NPC is currently exiting simple interaction.
    /// </summary>
    public partial class SimpleInteractionExitMarker : Element<NpcElement> {
        const float MaxDuration = 5f;

        public sealed override bool IsNotSaved => true;

        float _timeElapsed;

        public InteractionAnimationData InteractionAnimationData { get; }

        public SimpleInteractionExitMarker(InteractionAnimationData data) {
            InteractionAnimationData = data;
        }
        
        protected override void OnInitialize() {
            Location location = ParentModel.ParentModel;
            location.ListenTo(NpcElement.Events.AfterNpcOutOfVisualBand, _ => Discard(), this);
            location.GetOrCreateTimeDependent()?.WithUpdate(ProcessUpdate).ThatProcessWhenPause();
        }

        // === FailSafe if for some reason SimpleInteractionExitMarker won't be discarded
        void ProcessUpdate(float deltaTime) {
            NpcStateType? currentState = ParentModel.TryGetElement<NpcCustomActionsFSM>()?.CurrentAnimatorState?.Type;
            if (currentState.HasValue && currentState != NpcStateType.None) {
                return;
            }
            
            _timeElapsed += Time.unscaledDeltaTime;
            if (_timeElapsed > MaxDuration) {
                Discard();
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel?.ParentModel?.GetTimeDependent()?.WithoutUpdate(ProcessUpdate);
        }
    }
}