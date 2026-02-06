using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.VFX;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility.Debugging;
using Awaken.Utility.Times;
using Cysharp.Threading.Tasks;
using Unity.Burst.Intrinsics;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Pets.Variants {
    public abstract partial class PetVariantBase : Element<Location>, IRefreshedByAttachment<PetVariantAttachment>, IHeroActionBlocker {
        const float MinTimeLeftWhenBlocked = 2.0f;
        
        [Saved] float _timeLeft;
        [Saved] ARTimeSpan? _timeVariantWentInactive = null;
        [Saved] bool _petSpawned;
        [Saved] bool _spawnSequenceDone;
        [Saved] bool _followsTarget;

        PetVariantAttachment _spec;
        VCManualDissolveController _dissolveController;
        WeakModelRef<PetVariantBase> _transformationOtherVariantRef;
        bool _transforming;
        bool _ending;
        bool _inFeedSequence;
        
        protected abstract bool CanReduceTimeLeft { get; }
        protected PetVariantBase TransformationVariant => _transformationOtherVariantRef.Get();
        protected Hero PetOwner => Hero.Current;
        
        public virtual void InitFromAttachment(PetVariantAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnInitialize() {
            WaitForVisualLoaded(OnVisualLoaded);
            _timeLeft = _spec.duration;
        }

        protected override void OnRestore() {
            WaitForVisualLoaded(OnVisualLoaded);
            if (_petSpawned) {
                InitializeSpawnTransformation();
            }
        }

        protected override void OnFullyInitialized() {
            ParentModel.ListenTo(Events.BeforeDiscarded, OnBeforeLocationDiscard, this);
            
            if (_spec.hasDuration) {
                ParentModel.GetOrCreateTimeDependent().WithUpdate(OnUpdate);
                ParentModel.ListenTo(Location.Events.InteractabilityChanged, OnInteractabilityChanged, this);
            }
        }
        
        void OnInteractabilityChanged(LocationInteractability interactability) {
            var currentTime = World.Only<GameRealTime>().PlayRealTime;
            if (!interactability.interactable) {
                _timeVariantWentInactive = currentTime;
            } else if (_timeVariantWentInactive.HasValue) {
                var timeDiff = (currentTime - _timeVariantWentInactive.Value).TotalSeconds;
                _timeLeft = math.max(_timeLeft - timeDiff, 0f);
            }
        }

        void OnVisualLoaded() {
            _dissolveController = ParentModel.ViewParent.GetComponentInChildren<VCManualDissolveController>(true);
        }
        
        protected virtual void WaitForVisualLoaded(Action callback) {
            ParentModel.OnVisualLoaded(_ => callback());
        }

        void TransformFrom(PetVariantBase otherVariant, bool fast) {
            SetFollowing(otherVariant._followsTarget);
            
            if (otherVariant.ParentModel.HasElement<GameplayUniqueLocation>()) {
                GameplayUniqueLocation.InitializeForLocation(ParentModel);
            }
            
            if (!fast) {
                _transforming = true;
                _transformationOtherVariantRef = otherVariant;
                StartSpawn();
            }
        }

        void StartSpawn() {
            _spawnSequenceDone = false;
            _petSpawned = true;
            InitializeSpawnTransformation();
        }
        
        void InitializeSpawnTransformation() {
            if (!_spawnSequenceDone) {
                ParentModel.SetTemporaryScaleBasedInvisibility(true);
                WaitForVisualLoaded(OnSpawnReady);
            }
        }
        
        void OnSpawnReady() {
            if (_dissolveController != null) {
                _dissolveController.SwitchVisibility(false);
            }
            
            OnBeforeSpawn();
            SpawnSequence().Forget();
        }

        async UniTaskVoid SpawnSequence() {
            float spawnDelay = _spec.spawnDelayOnStart;
            if (TransformationVariant != null) {
                spawnDelay = math.max(spawnDelay, TransformationVariant._spec.spawnDelayOnStart);
            }
            
            if (!await AsyncUtil.DelayTime(this, spawnDelay)) {
                return;
            }
            
            var position = TransformationVariant?.ParentModel.Coords ?? ParentModel.Coords;
            var rotation = TransformationVariant?.ParentModel.Rotation ?? ParentModel.Rotation;
            
            if (_spec.variantSpawnVFX.IsSet) {
                await PrefabPool.InstantiateAndReturn(_spec.variantSpawnVFX, position, rotation);
            }
            
            if (!await AsyncUtil.DelayTime(this, _spec.spawnDelayAfterVfx)) {
                return;
            }

            ParentModel.SetTemporaryScaleBasedInvisibility(false);
            ParentModel.SafelyMoveAndRotateTo(position, rotation, true);

            OnSpawned();

            if (_dissolveController != null) {
                _dissolveController.SwitchVisibility(true);
            }
            _spawnSequenceDone = true;
            _transformationOtherVariantRef = null;
            _transforming = false;
        }
        
        void OnUpdate(float deltaTime) {
            if (_transforming) {
                return;
            }
            
            UpdateRemainingTimer(deltaTime);
        }
        
        void UpdateRemainingTimer(float deltaTime) {
            if (!CanReduceTimeLeft) {
                _timeLeft = math.max(_timeLeft, MinTimeLeftWhenBlocked);
                return;
            }
            
            _timeLeft -= deltaTime;
            if (_timeLeft <= 0f) {
                TransformIntoBaseVariant();
            }
        }

        protected virtual void OnBeforeSpawn() { }
        protected virtual void OnSpawned() { }
        protected virtual void OnBeforeEnd() { }
        protected virtual void OnEnded() { }
        protected virtual void OnPet() { }
        protected virtual void OnTaunt() { }
        protected virtual void OnFed() { }
        protected virtual void OnFollowStateChanged(bool state) { }

        public bool IsBlocked(Hero hero, IInteractableWithHero interactable) {
            return _inFeedSequence || _ending || _transforming;
        }
        
        public PetVariantBase TransformInto(TemplateReference variantTemplateRef, bool fast = false) {
            if (_transforming) {
                return null;
            }

            if (!variantTemplateRef.IsSet) {
                variantTemplateRef = CommonReferences.Get.PetBaseVariant;
            }

            var variantTemplate = variantTemplateRef.Get<LocationTemplate>();
            if (variantTemplate == null) {
                return null;
            }

            bool isCurrentTemplate = ParentModel.Template == variantTemplate;
            var qrkoMountTemplates = CommonReferences.Get.QrkoMountTemplates;
            bool bothAreMountVariants = qrkoMountTemplates.Contains(variantTemplate) && qrkoMountTemplates.Contains(ParentModel.Template);

            if (isCurrentTemplate || bothAreMountVariants) {
                ProlongTime();
                return null;
            }
            
            Location location = variantTemplate.SpawnLocation(ParentModel.Coords, ParentModel.Rotation);
            if (!location.TryGetElement<PetVariantBase>(out var petVariant)) {
                Log.Important?.Error($"Used location {location} without PetVariantBase as a pet variant for {ParentModel}");
                location.Discard();
                return null;
            }
            
            _transformationOtherVariantRef = petVariant;
            petVariant.TransformFrom(this, fast);
            this.End(fast);
            return petVariant;
        }

        public PetVariantBase TransformIntoBaseVariant(bool fast = false) {
            return TransformInto(CommonReferences.Get.PetBaseVariant, fast);
        }
        
        void ProlongTime() {
            _timeLeft += _spec.prolongDuration;
        }
        
        public void PerformPetting() {
            OnPet();
        }
        
        public void PerformTaunt() {
            OnTaunt();
        }

        public void SetFollowing(bool state) {
            _followsTarget = state;
            OnFollowStateChanged(state);
        }
        
        public void StartVariantFeedSequence(TemplateReference variantTemplate) {
            VariantFeedSequence(variantTemplate).Forget();
        }
        
        async UniTaskVoid VariantFeedSequence(TemplateReference variantTemplate) {
            const float FeedTime = 1f;

            _inFeedSequence = true;
            OnFed();
            if (!await AsyncUtil.DelayTime(this, FeedTime)) {
                return;
            }
            _inFeedSequence = false;
            TransformInto(variantTemplate);
        }
        
        void End(bool fast) {
            if (_transforming) {
                return;
            }
            _transforming = true;
            _ending = true;
            ParentModel.MarkedNotSaved = true;

            if (fast) {
                OnEnded();
                ParentModel.Discard();
                return;
            }
            
            if (TransformationVariant != null) {
                TransformationVariant.WaitForVisualLoaded(StartEndSequence);
            } else {
                StartEndSequence();
            }
        }

        void StartEndSequence() {
            if (this.HasBeenDiscarded) {
                return;
            }
            
            OnBeforeEnd();
            EndSequence().Forget();
        }
        
        async UniTaskVoid EndSequence() {
            float endDelay = _spec.disappearDelayOnEnd;
            if (TransformationVariant != null) {
                endDelay = math.max(endDelay, TransformationVariant._spec.disappearDelayOnEnd);
            }
            
            if (!await AsyncUtil.DelayTime(this, endDelay)) {
                return;
            }
            
            if (_spec.variantEndVFX.IsSet) {
                await PrefabPool.InstantiateAndReturn(_spec.variantEndVFX, ParentModel.Coords, ParentModel.Rotation);
            }
            
            if (!await AsyncUtil.DelayTime(this, _spec.disappearDelayAfterVfx)) {
                return;
            }
            
            if (_dissolveController != null) {
                _dissolveController.SwitchVisibility(false);
                if (!await AsyncUtil.DelayTime(this, _dissolveController.TotalDissolveTime)) {
                    return;
                }
            }
            
            OnEnded();
            ParentModel.Discard();
        }

        void OnBeforeLocationDiscard() {
            if (!ParentModel.WasDiscardedFromDomainDrop && !_ending) {
                TransformIntoBaseVariant(true);
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            base.OnDiscard(fromDomainDrop);
            ParentModel.GetTimeDependent()?.WithoutUpdate(OnUpdate);
        }

        public static bool TryGetCurrentlyActiveVariant(out PetVariantBase variant) {
            foreach (var checkedVariant in World.All<PetVariantBase>()) {
                if (!checkedVariant._ending) {
                    variant = checkedVariant;
                    return true;
                }
            }
            variant = null;
            return false;
        }
    }
}