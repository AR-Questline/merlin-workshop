using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Pets {
    public partial class PetElement : Element<Location>, IAliveAudio, IHeroActionBlocker, IRefreshedByAttachment<PetAttachment> {
        public override ushort TypeForSerialization => SavedModels.PetElement;

        [Saved] bool _followsTarget;
        [Saved] WeakModelRef<IGrounded> _targetToFollow;
        
        VCPetController _petController;
        bool _teleportOnVisualLoaded;
        
        public IGrounded TargetToFollow => _targetToFollow.Get();
        public bool ShouldFollowTarget => _followsTarget;
        public AliveAudio AliveAudio => ParentModel.TryGetElement<AliveAudio>();

        public void InitFromAttachment(PetAttachment spec, bool isRestored) { }

        protected override void OnInitialize() {
            InitializePet();
            
            if (!_targetToFollow.IsSet) {
                SetTargetToFollow(Hero.Current);
                SetFollowing(true);
            }
        }

        protected override void OnRestore() {
            InitializePet();
        }

        void InitializePet() {
            ParentModel.OnVisualLoaded(OnLocationVisualLoaded);

            ParentModel.ListenTo(GameplayUniqueLocation.Events.ChangedAvailability, OnGlobalExistenceAvailabilityChanged, this);
        }

        void OnGlobalExistenceAvailabilityChanged(bool state) {
            if (!ParentModel.IsVisualLoaded) {
                _teleportOnVisualLoaded = !state;
                return;
            }
            
            if (!state && TargetToFollow != null && ShouldFollowTarget) {
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
            return !_petController.CanInteractWith;
        }

        public void Pet() {
            var hero = Hero.Current;
            hero.Trigger(Hero.Events.HideWeapons, true);
            hero.Trigger(ToolInteractionFSM.Events.PatMount, hero);
            
            _petController.StartPet();
        }

        public void Taunt() {
            _petController.StartTaunt();
        }

        void SetTargetToFollow(IGrounded target) {
            if (_targetToFollow.Get() is {} existing) {
                World.EventSystem.RemoveAllListenersBetween(existing, this);
            }
            _targetToFollow = new WeakModelRef<IGrounded>(target);
            target.ListenTo(GroundedEvents.AfterTeleported, OnTargetTeleported, this);
        }

        void OnTargetTeleported(IGrounded obj) {
            if (ShouldFollowTarget) {
                _petController.TryTeleportNearTarget();
            }
        }

        public void SetFollowing(bool follow) {
            _followsTarget = follow;
        }

        public void TeleportIntoCurrentScene(Vector3 coords) {
            ParentModel.OnVisualLoaded(_ => _petController.Teleport(coords));
        }

        public bool HasBeenLeftBehind() {
            if (!ParentModel.Element<GameplayUniqueLocation>().InCurrentScene) {
                return true;
            }
            
            int distanceBand = ParentModel.GetCurrentBandSafe(LocationCullingGroup.LastBand);
            if (!LocationCullingGroup.InNpcVisibilityBand(distanceBand)) {
                return true;
            }

            return false;
        }

        public void PlayAudioClip(AliveAudioType audioType, bool asOneShot = false, params FMODParameter[] eventParams) {
            var eventReference = audioType.RetrieveFrom(this);
            if (!eventReference.IsNull) {
                ParentModel.LocationView.PlayAudioClip(eventReference, asOneShot, null, eventParams);
            }
        }
    }
}