using Awaken.Utility;
using System;
using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Crafting.Fireplace;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.Main.Heroes.MovementSystems;
using Awaken.TG.Main.Heroes.Resting;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Universal;
using Awaken.TG.Utility.Attributes;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Pathfinding;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class ItemHatching : Element<Item>, IItemAction, IRefreshedByAttachment<ItemHatchingAttachment> {
        public override ushort TypeForSerialization => SavedModels.ItemHatching;

        const float HatchTransitionTime = 1f;
        const float SpawnDistance = 1f;
        
        [Saved] int _stepsTakenWithItem;
        
        ItemHatchingAttachment _spec;
        Hero _ownerHero;
        IEventListener _ownerFootstepsListener;
        IEventListener _ownerJumpListener;
        IEventListener _restListener;
        bool _tryingToHatch;

        int RemainingStepsToHatch => math.max(0, _spec.OwnerStepsToHatch - _stepsTakenWithItem);
        bool CanStartHatching => RemainingStepsToHatch <= 0 && !_tryingToHatch;
        
        public ItemActionType Type => 
            _spec.HatchingMethod == HatchingMethod.ItemAction ? ItemActionType.Use : ItemActionType.Passive;
        
        public void InitFromAttachment(ItemHatchingAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnInitialize() {
            ParentModel.ListenTo(IItemOwner.Relations.OwnedBy.Events.AfterEstablished, OnOwnerAttached, this);
            ParentModel.ListenTo(IItemOwner.Relations.OwnedBy.Events.BeforeDisestablished, OnOwnerDetached, this);
            
            if (ParentModel.Owner != null) {
                OnOwnerAttached();
            }
        }

        void OnOwnerAttached() {
            if (ParentModel.Owner is Hero hero) {
                _ownerHero = hero;
                _ownerFootstepsListener = _ownerHero.ListenTo(Hero.Events.HeroFootstep, OnFootstep, this);
                _ownerJumpListener = _ownerHero.ListenTo(Hero.Events.HeroJumped, OnFootstep, this);
                _restListener = World.EventSystem.ListenTo(EventSelector.AnySource, RestPopupUI.Events.RestingStarted, this, OnRestingStarted);
            }
        }

        void OnFootstep() {
            _stepsTakenWithItem++;
            
            if (CanStartHatching && _spec.HatchingMethod == HatchingMethod.Automatic) {
                StartHatchingAutomatic().Forget();
            }
        }

        void OnRestingStarted(RestPopupUI restPopupUI) {
            if (!restPopupUI.IsSafelyResting || !World.HasAny<FireplaceUI>()) {
                return;
            }
            
            restPopupUI.ListenTo(RestPopupUI.Events.RestingInitiated, OnFireplaceRestInitiated, this);
        }

        void OnFireplaceRestInitiated() {
            if (CanStartHatching && _spec.HatchingMethod == HatchingMethod.FireplaceRest) {
                _tryingToHatch = true;
                StartHatchingFromRest().Forget();
            }
        }
        
        public void Submit() {
            if (CanStartHatching && _spec.HatchingMethod == HatchingMethod.ItemAction) {
                _tryingToHatch = true;
                World.Any<CharacterSheetUI>()?.Discard();
                StartHatching().Forget();
            }
        }
        public void AfterPerformed() { }
        public void Perform() { }
        public void Cancel() { }

        async UniTaskVoid StartHatchingAutomatic() {
            _tryingToHatch = true;
            if (!await AsyncUtil.WaitUntil(this, CanHatchAutomatically)) {
                _tryingToHatch = false;
                return;
            }
            StartHatching().Forget();
        }

        bool CanHatchAutomatically() {
            return _ownerHero != null && !_ownerHero.HeroCombat.IsHeroInFight && !_ownerHero.IsPortaling &&
                   _ownerHero.MovementSystem.Type == MovementType.Default && _ownerHero.Grounded &&
                   !_ownerHero.ShouldDie && !_ownerHero.IsUnderWater && _ownerHero.HeroCombat.EnemiesAlerted == 0;
        }

        async UniTaskVoid StartHatchingFromRest() {
            var presumedRestBlocker = World.Any<MapInteractabilityBlocker>();
            if (presumedRestBlocker is { HasBeenDiscarded: false }) {
                await AsyncUtil.WaitForDiscard(presumedRestBlocker);
                
                var transition = World.Services.Get<TransitionService>();
                transition.KillSequences();
                await transition.ToBlack(0);
            }
            
            HatchingSequence().Forget();
        }

        async UniTaskVoid StartHatching() {
            var transition = World.Services.Get<TransitionService>();
            await transition.ToBlack(HatchTransitionTime);
            
            HatchingSequence().Forget();
        }

        async UniTaskVoid HatchingSequence() {
            var transition = World.Services.Get<TransitionService>();
            
            var mapInteractabilityBlocker = World.Add(new MapInteractabilityBlocker());
            _ownerHero.Hide();
            
            GameCameraVoidOverride voidOverride = null;
            if (_spec.HatchingPlace == HatchingPlace.Void) {
                voidOverride = World.Add(new GameCameraVoidOverride(false));
            }

            var hatchingSequenceLocation = await SpawnHatchingSequence();

            if (hatchingSequenceLocation != null) {
                await transition.ToCamera(HatchTransitionTime);
                if (!await AsyncUtil.DelayTime(Hero.Current, _spec.HatchSequenceDuration)) {
                    return;
                }
                await transition.ToBlack(HatchTransitionTime);
            }
            
            await SpawnHatchingLocation();
            
            hatchingSequenceLocation?.Discard();
            voidOverride?.Discard();
            mapInteractabilityBlocker.Discard();
            _ownerHero.Show();
            ParentModel.Discard();
            
            await transition.ToCamera(HatchTransitionTime);
        }

        async UniTask<Location> SpawnHatchingSequence() {
            var template = _spec.HatchSequenceLocation;
            if (template == null) {
                return null;
            }

            var sequencePosition = _spec.HatchingPlace switch {
                HatchingPlace.Player => _ownerHero.ActorTransform.position,
                HatchingPlace.Void => Vector3.zero,
                _ => Vector3.zero
            };
            
            var hatchingSequenceLocation = template.SpawnLocation(sequencePosition);
            if (!await AsyncUtil.WaitUntil(this, () => hatchingSequenceLocation.IsVisualLoaded)) {
                hatchingSequenceLocation.Discard();
                return null;
            }

            return hatchingSequenceLocation;
        }

        async UniTask SpawnHatchingLocation() {
            var template = _spec.LocationToHatch;
            if (template == null) {
                return;
            }

            var spawnPoint = _ownerHero.ActorTransform.TransformPoint(Vector3.forward * SpawnDistance);
            NNInfo nnInfo = AstarPath.active.GetNearest(spawnPoint);
            var spawnPosition = nnInfo.node == null ? spawnPoint : nnInfo.position;
            var hatchedLocation = template.SpawnLocation(spawnPosition);

            if (_spec.HatchInGameplayDomain) {
                GameplayUniqueLocation.InitializeForLocation(hatchedLocation);
            }
            
            if (!await AsyncUtil.WaitUntil(this, () => hatchedLocation.IsVisualLoaded)) {
                hatchedLocation.Discard();
            }
        }
        
        void OnOwnerDetached() {
            World.EventSystem.TryDisposeListener(ref _ownerFootstepsListener);
            World.EventSystem.TryDisposeListener(ref _ownerJumpListener);
            World.EventSystem.TryDisposeListener(ref _restListener);
            _ownerHero = null;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            OnOwnerDetached();
            base.OnDiscard(fromDomainDrop);
        }

        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public static int GetRemainingStepsForItem(Item item) {
            return item.TryGetElement(out ItemHatching hatching) ? hatching.RemainingStepsToHatch : 0;
        }

        [Serializable]
        public enum HatchingMethod : byte {
            Automatic,
            ItemAction,
            FireplaceRest,
        }

        [Serializable]
        public enum HatchingPlace : byte {
            Void,
            Player,
        }
    }
}