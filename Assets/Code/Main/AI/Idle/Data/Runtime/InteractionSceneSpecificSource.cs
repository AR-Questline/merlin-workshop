using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Scenes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;

namespace Awaken.TG.Main.AI.Idle.Data.Runtime {
    public abstract partial class InteractionSceneSpecificSource : Element<IdleBehaviours>, IInteractionSource {
        [Saved] string _sceneName;
        [Saved] DeterministicInteractionFinder _finder;
        InteractionStartReason? _overridenStartReason;
        INpcInteraction _interaction;
        bool _justRestored;
        
        public virtual IInteractionFinder Finder => _finder;
        public InteractionStartReason? OverridenStartReason => _overridenStartReason;
        protected INpcInteraction Interaction => _interaction;
        protected NpcElement Npc => ParentModel.ParentModel;
        protected Location Location => Npc.ParentModel;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        protected InteractionSceneSpecificSource() {}

        protected InteractionSceneSpecificSource(DeterministicInteractionFinder finder, string sceneName = null, InteractionStartReason? startReason = null) {
            _finder = finder;
            _sceneName = sceneName ?? World.Services.Get<SceneService>()?.ActiveSceneRef?.Name;
            _overridenStartReason = startReason;
        }

        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, SceneLifetimeEvents.Events.SafeAfterSceneChanged, this, SceneChanged);
        }

        protected override void OnRestore() {
            World.EventSystem.ListenTo(EventSelector.AnySource, SceneLifetimeEvents.Events.SafeAfterSceneChanged, this, SceneChanged);
            _justRestored = true;
        }

        protected override void OnFullyInitialized() {
            if (SceneLifetimeEvents.Get.EverythingInitialized) {
                CheckScene(World.Services.Get<SceneService>()?.ActiveSceneRef?.Name, true);
            }
        }

        void SceneChanged() {
            CheckScene(World.Services.Get<SceneService>()?.ActiveSceneRef?.Name);
        }

        void CheckScene(string enteredSceneName, bool firstCheck = false) {
            if (enteredSceneName != _sceneName) {
                OnDifferentSceneEntered();
                _justRestored = false;
                return;
            }
            
            var interaction = Finder.FindInteraction(ParentModel);
            if (interaction == null) {
                Log.Important?.Error($"Interaction not found for scene {_sceneName} on scene {enteredSceneName}");
                OnCorrectSceneEnteredWithoutInteraction();
                _justRestored = false;
                return;
            }

            OnCorrectSceneEnteredWithInteraction(interaction, firstCheck);
            _justRestored = false;
        }
        
        protected abstract void OnDifferentSceneEntered();

        protected virtual void OnCorrectSceneEnteredWithoutInteraction() {
            DiscardAndRefresh();
        }

        protected virtual void OnCorrectSceneEnteredWithInteraction(INpcInteraction interaction, bool firstCheck) {
            BookInteraction(interaction);
            var startReason = firstCheck ? (_overridenStartReason ?? InteractionStartReason.ChangeInteraction) : InteractionStartReason.NPCActivated;
            ParentModel.RefreshCurrentBehaviour(true, startReason);
        }

        void BookInteraction(INpcInteraction interaction) {
            _interaction = interaction;
            var bookingResult = ParentModel.Book(_interaction);
            switch (bookingResult) {
                case InteractionBookingResult.ProperlyBooked:
                    _interaction.OnInternalEnd += OnInteractionEnd;
                    OnInteractionProperlyBooked();
                    break;
                case InteractionBookingResult.AlreadyBookedBySameNpc:
                    if (_justRestored) {
                        _interaction.OnInternalEnd += OnInteractionEnd;
                    }
                    return;
                default:
                    Log.Important?.Error($"Cannot book Unique Interaction {interaction} {bookingResult}");
                    break;
            }
            _justRestored = false;
        }
        
        protected abstract void OnInteractionProperlyBooked();
        
        void OnInteractionEnd() {
            if (Npc.HasBeenDiscarded || Npc.IsInCombat()) return;
            OnInteractionEnded();
            DiscardAndRefresh();
        }

        protected abstract void OnInteractionEnded();

        protected void DiscardAndRefresh() {
            var parent = ParentModel;
            Discard();
            parent.RefreshCurrentBehaviour();
        }
    }
}