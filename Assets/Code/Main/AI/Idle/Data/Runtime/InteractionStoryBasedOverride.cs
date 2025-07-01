using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.AI.Idle.Data.Runtime {
    public sealed partial class InteractionStoryBasedOverride : InteractionSceneSpecificSource {
        public override bool IsNotSaved => true;

        Story _story;
        bool _stopOnNpcInvolveEnded;
        IInteractionFinder _fallbackFinder;
        bool _useFallback;

        public bool Started { get; private set; } 

        public override IInteractionFinder Finder => _useFallback ? FallbackFinder : base.Finder;
        IInteractionFinder FallbackFinder => _fallbackFinder ??= new InteractionSpecificFinder(new StayInAbyssInteraction());

        public InteractionStoryBasedOverride(Story story, bool stopOnNpcInvolveEnded, DeterministicInteractionFinder finder, string sceneName = null, 
            InteractionStartReason? overridenStartReason = null) : base(finder, sceneName, overridenStartReason) {
            _story = story;
            _stopOnNpcInvolveEnded = stopOnNpcInvolveEnded;
        }

        protected override void OnInitialize() {
            base.OnInitialize();
            Npc.ListenTo(NpcInteractor.Events.InteractionChanged, OnInteractionChanged, this);
            _story.ListenTo(Events.AfterDiscarded, OnStoryDiscarded, this);
            if (_stopOnNpcInvolveEnded) {
                Npc.ListenTo(NpcInvolvement.Events.NpcInvolvementStopped, OnNpcInvolvementStopped, this);
            }
        }

        protected override void OnDifferentSceneEntered() {
            _useFallback = true;
            DiscardAndRefresh();
        }

        protected override void OnCorrectSceneEnteredWithoutInteraction() {
            _useFallback = true;
            base.OnCorrectSceneEnteredWithoutInteraction();
        }

        protected override void OnCorrectSceneEnteredWithInteraction(INpcInteraction interaction, bool firstCheck) {
            _useFallback = false;
            base.OnCorrectSceneEnteredWithInteraction(interaction, firstCheck);
        }

        void OnInteractionChanged(NpcInteractor.InteractionChangedInfo info) {
            if (info.changeType == NpcInteractor.InteractionChangedInfo.ChangeType.Start && info.interaction == Interaction) {
                Started = true;
            }
        }
        
        void OnStoryDiscarded() {
            DiscardAndRefresh();
        }
        
        void OnNpcInvolvementStopped(NpcInvolvement involvement) {
            DiscardAndRefresh();
        }
        
        protected override void OnInteractionProperlyBooked() {}
        
        protected override void OnInteractionEnded() { }
    }
}