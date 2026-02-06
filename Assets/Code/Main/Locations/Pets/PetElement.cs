using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Pets {
    public partial class PetElement : Element<Location>, IAliveAudio, IHeroActionBlocker,
        IRefreshedByAttachment<PetAttachment> {
        public override ushort TypeForSerialization => SavedModels.PetElement;

        [Saved] bool _followsTarget;
        [Saved] WeakModelRef<IGrounded> _targetToFollow;

        VCPetController _petController;
        bool _teleportOnVisualLoaded;
        DialogueAction _cachedDialogueAction;

        public Hero Owner => Hero.Current;
        public VCPetController Controller => _petController;
        public IGrounded TargetToFollow => _targetToFollow.Get();
        public bool WantsToFollowTarget => _followsTarget;
        public bool ShouldFollowTarget => WantsToFollowTarget;
        public AliveAudio AliveAudio => ParentModel.TryGetElement<AliveAudio>();
        public bool IsInDialogue => ParentModel.CachedElement(ref _cachedDialogueAction) is { IsInDialogue: true };
        public bool CanInteractWith => _petController is { CanInteractWith: true } && !IsInDialogue;

        public void InitFromAttachment(PetAttachment spec, bool isRestored) { }

        protected override void OnInitialize() {
            InitializePet();

            if (!_targetToFollow.IsSet) {
                SetTargetToFollow(Owner);
                SetFollowing(true);
            }
        }

        protected override void OnRestore() {
            InitializePet();
        }

        protected override void OnFullyInitialized() {
            if (_targetToFollow.TryGet(out var target)) {
                target.ListenTo(GroundedEvents.AfterTeleported, OnTargetTeleported, this);
            } else {
                Log.Critical?.Error($"Pet Element has not target to follow: {LogUtils.GetDebugName(ParentModel)}");
            }
        }

        void InitializePet() {
            ParentModel.OnVisualLoaded(OnLocationVisualLoaded);

            ParentModel.ListenTo(GameplayUniqueLocation.Events.ChangedAvailability,
                OnGlobalExistenceAvailabilityChanged, this);
        }

        void OnGlobalExistenceAvailabilityChanged(bool state) {
            if (!ParentModel.IsVisualLoaded) {
                _teleportOnVisualLoaded = !state;
                return;
            }

            if (!state && TargetToFollow != null && ShouldFollowTarget && !World.Any<LoadingScreenUI>()) {
                _petController.TryTeleportNearTarget();
            }
        }

        void OnLocationVisualLoaded(Transform t) {
            _petController = t.GetComponentInChildren<VCPetController>();
            _petController.Initialize();

            if (_teleportOnVisualLoaded && TargetToFollow != null && ShouldFollowTarget) {
                _petController.TryTeleportNearTarget();
            }
        }

        public bool IsBlocked(Hero hero, IInteractableWithHero interactable) {
            return !CanInteractWith;
        }

        public void Pet() {
            var hero = Hero.Current;
            hero.Trigger(Hero.Events.HideWeapons, true);
            hero.Trigger(ToolInteractionFSM.Events.PatMount, hero);
            _petController.Animancer.PlayAnimationState(ARPetAnimancer.State.Pet);
        }

        public void Taunt() {
            _petController.Animancer.PlayAnimationState(ARPetAnimancer.State.Taunt);
        }

        void SetTargetToFollow(IGrounded target) {
            if (_targetToFollow.Get() is { } existing) {
                World.EventSystem.RemoveAllListenersBetween(existing, this);
            }

            _targetToFollow = new WeakModelRef<IGrounded>(target);
            if (IsFullyInitialized) {
                target.ListenTo(GroundedEvents.AfterTeleported, OnTargetTeleported, this);
            }
        }

        void OnTargetTeleported(IGrounded obj) {
            if (ShouldFollowTarget) {
                if (World.Any<LoadingScreenUI>() is { } loadingScreenUI) {
                    if ((loadingScreenUI.PreviousScene?.IsAdditive ?? false) && !loadingScreenUI.MapSceneAlreadySetup) {
                        // loading new Main Scene from additive has different setup cycle than returning to Main Scene from additive.
                        World.EventSystem.LimitedListenTo(EventSelector.AnySource,
                            SceneLifetimeEvents.Events.PathfindingRestored, this,
                            _ => _petController.TryTeleportNearTarget(), 1);
                    } else if (ParentModel.TryGetElement(out GameplayUniqueLocation gameplayUniqueLocation)) {
                        gameplayUniqueLocation.SetCurrentScene(World.Services.Get<SceneService>().ActiveSceneRef?.Name);
                        gameplayUniqueLocation.SetCurrentPosition(NpcPresence.AbyssPosition);
                    }

                    return;
                }

                _petController.TryTeleportNearTarget();
            }
        }

        public void SetFollowing(bool follow) {
            _followsTarget = follow;
        }

        public void Recall(Vector3 coords) {
            SetFollowing(true);
            ParentModel.OnVisualLoaded(_ => _petController.Teleport(coords));
        }

        public void PlayAudioClip(AliveAudioType audioType, bool asOneShot = false,
            params FMODParameter[] eventParams) {
            var eventReference = audioType.RetrieveFrom(this);
            if (!eventReference.IsNull) {
                ParentModel.LocationView.PlayAudioClip(eventReference, asOneShot, null, eventParams);
            }
        }
    }
}