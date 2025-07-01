using System;
using System.Collections.Generic;
using System.Threading;
using Awaken.Kandra;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.Animations;
using Awaken.TG.Graphics.Cutscenes;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.FPP;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Animations;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.UI.RawImageRendering;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.Debugging;
using Awaken.Utility.Enums;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Awaken.TG.Main.Heroes.Combat {
    [RequireComponent(typeof(ARFmodEventEmitter))]
    public abstract class CharacterHandBase : View<Item> {
        const uint DefaultLightLayerMask = 257;
        
        // === Fields
        protected bool _isLoadingAnimator;
        protected Animator _animator;
        protected bool _overridesRemoved = true;
        protected ARHeroStateToAnimationMapping _overrideController;
        protected List<ARHeroStateToAnimationMapping> _additionalOverrides = new();
        protected WeaponEventsListener _weaponEventsListener;
        bool _listenersAttached, _wasAnimatorOverrideLoaded;
        List<ARAssetReference> _loadedAnimatorOverrides = new();
        bool _hasBeenDiscarded;

        // === Properties
        public bool IsLoadingAnimator => _isLoadingAnimator;
        public virtual Transform VisualFirePoint => null;
        public Item Item => Target;
        public IItemOwner Owner => Item?.Owner;
        public bool IsHidden => !isActiveAndEnabled;
        public IHandOwner<ICharacter> HandOwner { get; private set; }
        protected IEnumerable<string> Layers() => AnimatorUtils.FppArmsLayers();
        protected ARHeroAnimancer HeroAnimancer => Hero.Current.VHeroController.Animancer;

        // === Audio
        ARFmodEventEmitter _emitter;
        
        // === Events
        public static class Events {
            public static readonly Event<IItemOwner, bool> WeaponVisibilityToggled = new(nameof(WeaponVisibilityToggled));
            public static readonly Event<Item, CharacterHandBase> WeaponAttached = new(nameof(WeaponAttached));
            public static readonly Event<Item, CharacterHandBase> WeaponDestroyed = new(nameof(WeaponDestroyed));
        }

        // === Unity LifeCycle
        protected virtual void OnEnable() {
            // Weapons don't need any colliders since they are using BoxCasts to detect hits, but enabled colliders can trigger bugs like blocking AI vision.
            foreach (var col in GetComponentsInChildren<Collider>()) {
                col.enabled = false;
            }
        }

        // === Initialization
        protected override void OnInitialize() {
            _emitter = GetComponent<ARFmodEventEmitter>();
            //_emitter.EventStopTrigger = EmitterGameEvent.ObjectDisable;
            
            if (Owner is not HeroRenderer) {
                var meshRenderers = GetComponentsInChildren<MeshRenderer>();
                foreach (var meshRenderer in meshRenderers) {
                    meshRenderer.renderingLayerMask = DefaultLightLayerMask;
                }

                var kandraRenderers = GetComponentsInChildren<KandraRenderer>();
                foreach (var kandraRenderer in kandraRenderers) {
                    var filterSettings = kandraRenderer.rendererData.filteringSettings;
                    if (filterSettings.renderingLayersMask != DefaultLightLayerMask) {
                        filterSettings.renderingLayersMask = DefaultLightLayerMask;
                        kandraRenderer.SetFilteringSettings(filterSettings);
                    }
                }
            }
            
            ICharacter ownerCharacter = Owner?.Character;
            InitListeners(ownerCharacter);
            
            switch (ownerCharacter) {
                case Hero hero:
                    InitializeForHero(hero);
                    break;
                case NpcElement npc:
                    InitializeForNpc(npc);
                    break;
            }
            ShowWeapon();
        }

        protected override void OnMount() {
            Target.Trigger(Events.WeaponAttached, this);
        }

        void InitializeForHero(Hero hero) {
            LayerMask layersToKeep = RenderLayers.Mask.VFX | RenderLayers.Mask.PostProcessing;
            
            foreach (Transform child in gameObject.GetComponentsInChildren<Transform>(true)) {
                var childObject = child.gameObject;
                if (layersToKeep.Contains(childObject.layer)) {
                    continue;
                }
                childObject.layer = RenderLayers.IgnoreRaycast;
            }
            
            _animator = GetComponentInParent<Animator>(true);
            HeroWeaponEvents.Current.RegisterWeapon(this);
            HandOwner = hero.Element<HeroHandOwner>();
            OnInitializedForHero(hero);
        }

        void InitializeForNpc(NpcElement npc) {
            _animator = GetComponentInParent<Animator>(true);
            HandOwner = npc.Element<NpcHandOwner>();
            OnInitializedForNpc(npc);
        }

        void AttachToHero(Hero hero) {
            if (!hero.CanUseEquippedWeapons) {
                hero.Trigger(Hero.Events.HideWeapons, true);
                return;
            }
            hero.Trigger(Hero.Events.OnWeaponBeginEquip, true);
            OnAttachedToHero(hero);
        }
        void AttachToNpc(NpcElement npc) {
            OnAttachedToNpc(npc);
        }

        void InitListeners(ICharacter character) {
            if (_listenersAttached) {
                return;
            }

            character.ListenTo(IAlive.Events.BeforeDeath, OnOwnerDeath, this);
            character.ListenTo(ICharacter.Events.OnEffectInvokedAnimationEvent, OnEffectInvoked, this);
            if (character is Hero hero) {
                hero.ListenTo(Hero.Events.ShowWeapons, ShowWeapon, this);
                hero.ListenTo(Hero.Events.HideWeapons, HideWeapon, this);
            }
            _listenersAttached = true;
        }

        void OnOwnerDeath(DamageOutcome damageOutcome) {
            if (HasBeenDiscarded || Target.HiddenOnUI || Owner is Hero) {
                return;
            }

            var data = new DroppedItemData {
                item = Target,
                quantity = Target.Quantity,
            };
            Vector3 ragdollForce = damageOutcome.RagdollForce;
            if (Owner.Character is NpcElement npcElement) {
                ragdollForce = damageOutcome.RagdollForce / npcElement.Template.npcWeight;
            }
            Location location = DroppedItemSpawner.SpawnDroppedItemPrefab(transform.position, data, transform.rotation, ragdollForce);
            location.AddElement(new NPCItemDroppedElement(Target));
            gameObject.SetActive(false);
        }
        
        void OnEffectInvoked(ARAnimationEventData eventData) {
            if (eventData.restriction.Match(this) && Owner is ICharacter character) {
                character.Trigger(ICharacter.Events.OnEffectInvoked, Item);
            }
        }
        protected virtual void OnInitializedForHero(Hero hero) { }
        protected virtual void OnInitializedForNpc(NpcElement npcElement) { }
        protected virtual void OnAttachedToHero(Hero hero) { }
        protected virtual void OnAttachedToNpc(NpcElement npcElement) { }
        protected virtual void OnAttachedToCustomHeroClothes(CustomHeroClothes clothes, ItemEquip equip) { }
        protected virtual void OnDetachedFromCustomHeroClothes(CustomHeroClothes clothes) { }
        
        CancellationTokenSource _loadingAnimatorController;
        
        protected async UniTaskVoid LoadAnimatorController(ARAssetReference animatorControllerRef, params ARAssetReference[] additionalOverrides) {
            if (_isLoadingAnimator || this == null) {
                #if !UNITY_EDITOR
                Log.Critical?.Error("Loading animator controller again: isLoaded:" + _wasAnimatorOverrideLoaded + " isLoading: " + _isLoadingAnimator + " " + LogUtils.GetDebugName(Target));
                #endif
                return;
            }
            
            _isLoadingAnimator = true;
            gameObject.SetActive(false);
            
            if (!(animatorControllerRef?.IsSet ?? false)) {
                Log.Important?.Warning($"Null animation controller for: {gameObject.name}");
                return;
            }

            _overridesRemoved = false;
            _loadedAnimatorOverrides.Add(animatorControllerRef);

            var tasks = new List<ARAsyncOperationHandle<ARHeroStateToAnimationMapping>>(1 + (additionalOverrides?.Length ?? 0));

            var mainAnimator = animatorControllerRef.LoadAsset<ARHeroStateToAnimationMapping>();
            tasks.Add(mainAnimator);
            mainAnimator.OnCompleteForceAsync(h => {
                if (h.Status == AsyncOperationStatus.Succeeded) {
                    OnAnimatorOverrideLoaded(h.Result);
                } else {
                    Log.Important?.Error($"Failed to load AnimatorOverrideController for weapon: {gameObject.name}");
                }
            });

            if (additionalOverrides != null) {
                foreach (var additionalOverride in additionalOverrides) {
                    if (additionalOverride?.IsSet ?? false) {
                        _loadedAnimatorOverrides.Add(additionalOverride);
                        var additionalOverrideMapping = additionalOverride.LoadAsset<ARHeroStateToAnimationMapping>();
                        tasks.Add(additionalOverrideMapping);
                        additionalOverrideMapping.OnCompleteForceAsync(h => {
                            if (h.Status == AsyncOperationStatus.Succeeded) {
                                OnAdditionalOverrideLoaded(h.Result);
                            }
                        });
                    }
                }
            }
            if (_loadingAnimatorController != null) {
                _loadingAnimatorController.Cancel();
                Log.Critical?.Error($"Loading animator when already loading: {LogUtils.GetDebugName(Target)}");
            }
            _loadingAnimatorController = new CancellationTokenSource();
            CancellationToken cancellationToken = _loadingAnimatorController.Token;
            
            while (tasks.Count > 0) {
                await AsyncUtil.DelayFrame(this, 1, cancellationToken);
                if (cancellationToken.IsCancellationRequested) {
                    tasks.Clear();
                    return;
                }
                for (int i = tasks.Count - 1; i >= 0; i--) {
                    if (!tasks[i].IsValid() || tasks[i].Status != AsyncOperationStatus.None) {
                        tasks.RemoveAt(i);
                    }
                }
            }

            _isLoadingAnimator = false;
            _loadingAnimatorController = null;
            
            if (HasBeenDiscarded) {
                return;
            }
            if (await AsyncUtil.DelayFrame(this)) {
                AfterAnimatorLoaded();
            }
        }

        void OnAnimatorOverrideLoaded(ARHeroStateToAnimationMapping overrideController) {
            if (this == null || HasBeenDiscarded) {
                return;
            }

            gameObject.SetActive(true);
            _overrideController = overrideController;
            HeroAnimancer.ApplyOverrides(this, overrideController);
        }

        void OnAdditionalOverrideLoaded(ARHeroStateToAnimationMapping overrideController) {
            if (this == null || HasBeenDiscarded) {
                return;
            }
            
            _additionalOverrides.Add(overrideController);
            HeroAnimancer.ApplyOverrides(this, overrideController);
        }
        
        protected virtual void AfterAnimatorLoaded() {
            if (_overridesRemoved) {
                #if !UNITY_EDITOR
                Log.Critical?.Error($"Overrides were removed while loading animator controller for weapon: {gameObject.name} {LogUtils.GetDebugName(Target)}");
                #endif
                return;
            }
            if (Owner?.Character is Hero) {
                Owner?.Trigger(Events.WeaponVisibilityToggled, true);
                if (!_wasAnimatorOverrideLoaded && Owner is Hero h) {
                    bool bothWeaponsFists = h.MainHandItem is { IsDefaultFists: true } && h.OffHandItem is { IsDefaultFists: true };
                    if (bothWeaponsFists || !h.CanUseEquippedWeapons) {
                        HideWeapon(true);
                        _wasAnimatorOverrideLoaded = true;
                        return;
                    }
                }
                ToggleAnimatorLayers(true);
                _wasAnimatorOverrideLoaded = true;
            } else if (Owner?.Character is NpcElement npcElement) {
                npcElement.Trigger(EnemyBaseClass.Events.AfterWeaponFullyLoaded, this);
            }
        }

        protected abstract void ToggleAnimatorLayers(bool activate);

        protected virtual void OnWeaponHidden() {
            if (HandOwner == null) {
                return;
            }

            UnloadAnimatorOverrides();

            if (Owner?.Character is Hero) {
                ToggleAnimatorLayers(false);
                Owner.Trigger(Events.WeaponVisibilityToggled, false);
            }
        }
        
        protected virtual void AttachWeaponEventsListener() {
            if (Owner is Hero) {
                return;
            }
            
            if (_weaponEventsListener != null) {
                return;
            }
            
            Animator animator = GetComponentInParent<Animator>(true);
            if (animator == null) {
                return;
            }
            
            _weaponEventsListener = animator.GetComponent<WeaponEventsListener>();
            if (_weaponEventsListener == null) {
                _weaponEventsListener = animator.AddComponent<WeaponEventsListener>();
            }
            if (_weaponEventsListener != null) {
                _weaponEventsListener.InitWeapon(this);
            }
        }
        
        public void AttachToCustomHeroClothes(CustomHeroClothes clothes, ItemEquip itemEquip) {
            OnAttachedToCustomHeroClothes(clothes, itemEquip);
        }

        public void DetachFromCustomHeroClothes(CustomHeroClothes clothes) {
            OnDetachedFromCustomHeroClothes(clothes);
        }
        
        // === Toggling Weapon Visibility
        public virtual void ShowWeapon() {
            switch (Owner?.Character) {
                case Hero hero:
                    AttachToHero(hero);
                    break;
                case NpcElement npc:
                    AttachToNpc(npc);
                    break;
            }
        }

        public virtual void HideWeapon(bool instantHide) {
            if (Owner?.Character is not Hero hero) {
                return;
            }

            bool shouldPrevent = !instantHide && hero.Elements<HeroAnimatorSubstateMachine>().Any(fsm => fsm.PreventHidingWeapon);
            if (shouldPrevent) {
                return;
            }
            
            hero.Trigger(Hero.Events.OnWeaponBeginUnEquip, true);
            VFXUtils.StopVfx(gameObject);
            if (instantHide) {
                OnUnEquippingEnded();
            }
        }

        public void OnUnEquippingEnded() {
            gameObject.SetActive(false);
            OnWeaponHidden();
        }

        public void ChangeHeroPerspective(bool tpp) {
            HideWeapon(true);
            ShowWeapon();
        }

        protected abstract void LoadHeroAnimatorOverrides();

        protected void UnloadAnimatorOverrides() {
            if (!_overridesRemoved) {
                Log.Debug?.Warning($"Overrides being removed: {LogUtils.GetDebugName(Target)} animator loading: {_isLoadingAnimator} animator loaded: {_wasAnimatorOverrideLoaded}");
                _overridesRemoved = true;
                if (_overrideController != null) {
                    HeroAnimancer.RemoveOverrides(this, _overrideController);
                }

                _additionalOverrides.ForEach(overrideController => HeroAnimancer.RemoveOverrides(this, overrideController));
                _additionalOverrides.Clear();
                _loadedAnimatorOverrides.ForEach(o => o.ReleaseAsset());
                _loadedAnimatorOverrides.Clear();
                _loadingAnimatorController?.Cancel();
                _loadingAnimatorController = null;
                _isLoadingAnimator = false;
            } else {
                Log.Debug?.Info($"Overrides already removed: {LogUtils.GetDebugName(Target)} animator loading: {_isLoadingAnimator} animator loaded: {_wasAnimatorOverrideLoaded}");
            }
        }

        // === Tool Interactions
        public virtual void OnToolInteractionStart() { }

        public virtual void OnToolInteractionEnd() { }

        /// <summary>
        /// Since view is attached to 3D prefab of location it can be destroyed without being discarded.
        /// So we need to make sure that it will discard on destroy.
        /// </summary>
        void OnDestroy() {
#if UNITY_EDITOR
            if (EditorOnly.Utils.EditorApplicationUtils.IsLeavingPlayMode) {
                return;
            }
#endif
            if (_hasBeenDiscarded) {
                return;
            }
            bool targetDiscarded = Target?.HasBeenDiscarded ?? true;
            if (!targetDiscarded) {
                Discard();
            }
        }

        protected override IBackgroundTask OnDiscard() {
            _hasBeenDiscarded = true;
            OnWeaponHidden();

            _weaponEventsListener?.Clear(this);
            _weaponEventsListener = null;
            HeroWeaponEvents.Current.UnregisterWeapon(this);

            if (Target == null) {
                Debug.LogException(
                    new NullReferenceException(
                        $"Character Hand Base discard happened with null Target. It means that View wasn't initialized properly. {gameObject.name}"),
                    gameObject);
            } else {
                Target.Trigger(Events.WeaponDestroyed, this);
            }

            return base.OnDiscard();
        }

        // === Audio
        public void PlayAudioClip(ItemAudioType itemAudioType, bool asOneShot = false, params FMODParameter[] eventParams) {
            PlayAudioClip(itemAudioType.RetrieveFrom(Item), asOneShot, eventParams);
        }
        
        public void PlayAudioClip(EventReference eventReference, bool asOneShot = false, params FMODParameter[] eventParams) {
            if (this == null) {
                return;
            }

            // if (asOneShot) {
            //     FMODManager.PlayAttachedOneShotWithParameters(eventReference, gameObject, _emitter, eventParams);
            // } else if (eventReference.IsNull) {
            //     _emitter.ChangeEvent(new EventReference());
            // } else {
            //     _emitter.PlayNewEventWithPauseTracking(eventReference, eventParams);
            // }
        }

        public void PauseAudioClip() {
            //_emitter.Pause();
        }

        public void UnpauseAudioClip() {
            //_emitter.UnPause();
        }

        public void SetParameter(string paramName, float value) {
            if (this == null) {
                return;
            }
            //_emitter.SetParameter(paramName, value);
        }
    }
}