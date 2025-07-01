using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Utility.StateMachines;
using Awaken.TG.MVC;
using Awaken.Utility;

namespace Awaken.TG.Main.AI.Idle.Data.Runtime {
    public sealed partial class InteractionOneShot : InteractionSceneSpecificSource {
        public override bool IsNotSaved => true;

        readonly bool _discardOnPause;

        public InteractionOneShot(bool discardOnPause, DeterministicInteractionFinder finder, string sceneName = null) : base(finder, sceneName) {
            _discardOnPause = discardOnPause;
        }

        protected override void OnDifferentSceneEntered() {
            DiscardAndRefresh();
        }

        protected override void OnInteractionProperlyBooked() {
            if (_discardOnPause) {
                Npc.ListenTo(NpcInteractor.Events.InteractionChanged, OnInteractionChanged, this);
            }
            Npc.ListenTo(NpcAI.Events.NpcStateChanged, OnStateChange, this);
        }
        
        void OnInteractionChanged(NpcInteractor.InteractionChangedInfo changedInfo) {
            // If One Shot is paused it shouldn't be resumed.
            // Stopping is handled by the InteractionSceneSpecificSource.
            if (changedInfo.changeType == NpcInteractor.InteractionChangedInfo.ChangeType.Pause && changedInfo.interaction == Interaction) {
                DiscardAndRefresh();
            }
        }
        
        void OnStateChange(Change<IState> change) {
            if (change is (StateAIWorking, StateAINotWorking)) {
                DiscardAndRefresh();
            }
        }
        
        protected override void OnInteractionEnded() {}
    }
}