using System;
using System.Collections.Generic;
using System.Threading;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Mobs;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Extensions;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Utility.VFX {
    public abstract class VCDissolveControllerBase<T> : ViewComponent<Location> {
        protected const string AdditionalDissolveEffectsGroupName = "AdditionalDissolveEffects";
        static readonly List<T> GetComponentsBuffer = new();

        [SerializeField] protected bool inverseTransition;
        [SerializeField] protected DissolveType dissolveType = DissolveType.All;
        [SerializeField, SuffixLabel("s", overlay: true)] float dissolveDuration = 2.5f;
        [SerializeField, SuffixLabel("ms", overlay: true)] int delayBeforeDisappear = 1000;
        [SerializeField, BoxGroup(AdditionalDissolveEffectsGroupName)] DrakeAnimatedPropertiesOverrideController[] drakeControllersToTrigger = Array.Empty<DrakeAnimatedPropertiesOverrideController>();

        [ShowInInspector]
        protected List<T> _actualRenderers = new();
        CancellationTokenSource _cts = new();
        protected bool _discardOnDisappeared;
        float _transition;
        bool _isDisappearing;

        public bool isInEffect;

        public float TotalDissolveTime => dissolveDuration + delayBeforeDisappear / 1000f;
        protected virtual float Invisible => inverseTransition ? 0 : 1;
        protected virtual float Visible => inverseTransition ? 1 : 0;
        bool ShouldBeInDissolvedState => isInEffect || _transition != Visible;
        bool IsCanceledOrDisappearing => _isDisappearing || _cts.IsCancellationRequested;

        protected override void OnAttach() {
            var npc = Target.TryGetElement<NpcElement>();
            // --- Clothes
            if (Target.TryGetElement(out NpcClothes clothes)) {
                clothes.ListenTo(BaseClothes.Events.ClothEquipped, OnClothLoaded, this);
                clothes.ListenTo(BaseClothes.Events.ClothBeingUnequipped, OnClothUnequipped, this);
            } else if (npc != null) {
                npc.OnCompletelyInitialized(_ => {
                    clothes = Target.Element<NpcClothes>();
                    clothes.ListenTo(BaseClothes.Events.ClothEquipped, OnClothLoaded, this);
                    clothes.ListenTo(BaseClothes.Events.ClothBeingUnequipped, OnClothUnequipped, this);
                });
            }
            // --- Weapons
            Target.TryGetElement<NpcItems>()?.ListenTo(ItemEquip.Events.WeaponEquipped, OnWeaponLoaded, this);
            npc?.ListenTo(NpcWeaponsHandler.Events.NpcWeaponLoaded, OnWeaponLoaded, this);
        }

        public void FindAdditionalRenderers(Transform transform) {
            using var buffer = new ReusableListBuffer<T>(GetComponentsBuffer);
            transform.GetComponentsInChildren<T>(buffer);
            foreach (var dissolvable in buffer) {
                AddRenderer(dissolvable);
            }
        }
        
        public void RemoveRenderer(T renderer) {
            _actualRenderers.Remove(renderer);
        }
        
        void OnWeaponLoaded(GameObject weapon) {
            if (!dissolveType.HasFlagFast(DissolveType.Weapon)) {
                return;
            }
            using var buffer = new ReusableListBuffer<T>(GetComponentsBuffer);
            weapon.GetComponentsInChildren<T>(buffer);
            foreach (var dissolvable in buffer) {
                AddRenderer(dissolvable);
            }
        }
        
        void OnClothLoaded(GameObject cloth) {
            if (!dissolveType.HasFlagFast(DissolveType.Cloth)) {
                return;
            }
            FindAdditionalRenderers(cloth.transform);
            if (ShouldBeInDissolvedState) {
                UpdateEffects(_transition);
            }
        }
        
        void OnClothUnequipped(GameObject cloth) {
            if (!dissolveType.HasFlagFast(DissolveType.Cloth)) {
                return;
            }
            using var buffer = new ReusableListBuffer<T>(GetComponentsBuffer);
            cloth.GetComponentsInChildren<T>(buffer);
            foreach (var dissolvable in buffer) {
                RemoveRenderer(dissolvable);
            }
        }

        protected void Appear(Transform parentTransform) {
            FindAdditionalRenderers(parentTransform);
            Appear();
        }

        protected void Appear() {
            CancelDissolveEffects();
            BeforeAppeared();
            DissolveEffect(Invisible, Visible, _cts.Token).Forget();
        }

        protected virtual void BeforeAppeared() {
            foreach (var drakeAnimatedPropertiesOverrideController in drakeControllersToTrigger) {
                drakeAnimatedPropertiesOverrideController.StartBackward();
            }
        }

        protected async UniTaskVoid StartDisappear() {
            await UniTask.Delay(delayBeforeDisappear, cancellationToken: _cts.Token);
            if (_cts.IsCancellationRequested) {
                return;
            }

            Disappear().Forget();
        }

        protected async UniTask Disappear() {
            if (IsCanceledOrDisappearing) {
                return;
            }
            
            CancelDissolveEffects();
            _isDisappearing = true;
            BeforeDisappeared();
            await DissolveEffect(Visible, Invisible, _cts.Token);
            _isDisappearing = false;
            
            if (_discardOnDisappeared && Target is { HasBeenDiscarded: false }) {
                Target.Discard();
            }
        }
        
        protected virtual void BeforeDisappeared() {
            foreach (var drakeAnimatedPropertiesOverrideController in drakeControllersToTrigger) {
                drakeAnimatedPropertiesOverrideController.StartForward();
            }
        }

        async UniTask DissolveEffect(float startValue, float endValue, CancellationToken ct) {
            isInEffect = true;
            _transition = startValue;

            foreach (var dissolveAbleRenderer in _actualRenderers) {
                BeforeDissolveStarted(dissolveAbleRenderer, startValue);
            }

            UpdateEffects(startValue);
            OnDissolveStarted();
            float duration = 0f;
            while (await AsyncUtil.DelayFrame(this, 1, ct)) {
                duration += Time.deltaTime;
                if (duration >= dissolveDuration) {
                    UpdateTransition(endValue);
                    break;
                }
                UpdateTransition(math.lerp(startValue, endValue, duration / dissolveDuration));
            }

            if (ct.IsCancellationRequested || HasBeenDiscarded) {
                return;
            }

            UpdateEffects(endValue);
            AfterDissolveEnded(endValue, ct);
            isInEffect = false;
        }
        
        /// <param name="startingTransitionValue">1 means dissolved and 0 means fully visible. It's made this way because of dissolve shader</param>
        protected virtual void BeforeDissolveStarted(T renderer, float startingTransitionValue) { }

        protected virtual void OnDissolveStarted() { }

        protected virtual void AfterDissolveEnded(float endValue, CancellationToken ct) { }

        protected void UpdateTransition(float transition) {
            _transition = transition;
            UpdateEffects(_transition);
        }

        /// <param name="transition">1 means dissolved and 0 means fully visible. It's made this way because of dissolve shader</param>
        protected abstract void UpdateEffects(float transition);

        protected void AddRenderer(T renderer) {
            if (!CanBeDissolved(renderer)) {
                return;
            }
            if (!_actualRenderers.Contains(renderer)) {
                _actualRenderers.Add(renderer);
                OnRendererAdded(renderer);
                if (ShouldBeInDissolvedState) {
                    BeforeDissolveStarted(renderer, _transition);
                }
            }
        }

        protected virtual void OnRendererAdded(T dissolveable) {}

        protected abstract bool CanBeDissolved(T dissolvable);
        
        void CancelDissolveEffects(bool createNew = true) {
            _cts.Cancel();
            _cts.Dispose();
            isInEffect = false;
            if (createNew) {
                _cts = new CancellationTokenSource();
            }
        }

        protected override void OnDiscard() {
            CancelDissolveEffects(false);
        }
    }
}