using System;
using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.UI.HeroCreator.ViewComponents;
using Awaken.TG.Main.UI.HeroRendering;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.GameObjects;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.UI.RawImageRendering {
    [UsesPrefab("UI/RawImageRendering/VHeroRenderer")]
    public class VHeroRenderer : VHeroRendererBase<HeroRenderer> {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [Title("3D Environment")] 
        [SerializeField] Transform enviroHost;
        [SerializeField] CinemachineVirtualCamera enviroCam;
        [SerializeField] Material foregroundQuadMaterial;
        
        [SerializeField, BoxGroup("Camera")] Transform cameraTransform;
        [SerializeField, BoxGroup("Camera")] float cameraTransitionTime;
        [SerializeField, BoxGroup("Camera")] ViewTargets viewTargets;
        
        Tween _fadeTween;
        RotatableObject _rotatable;
        HeroRenderer.Target _currentTarget;
        Sequence _cameraSequence;
        bool _equipmentChanged;

        [UnityEngine.Scripting.Preserve] PartialVisibility _heroVisibility = PartialVisibility.Visible;
        
        public Camera Camera => World.Only<GameCamera>().MainCamera;
        protected override int BodyInstanceLayer => RenderLayers.UI;

        // === Initialization
        protected override void OnInitialize() {
            base.OnInitialize();
            TeleportEnviroToHeroPosition().Forget();
            
            if (Target.UseLoadoutAnimations) {
                InitializeLoadoutEvents();
            }
        }
        
        async UniTaskVoid TeleportEnviroToHeroPosition() {
            enviroHost.position = Hero.Current?.Coords ?? Vector3.zero;
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            enviroCam.Priority = 9999;
        }

        void InitializeLoadoutEvents() {
            var heroItems = Target.Hero.HeroItems;
            if (heroItems != null) {
                heroItems.ListenTo(HeroLoadout.Events.LoadoutChanged, OnLoadoutChanged, this);
                foreach (var loadout in heroItems.Loadouts) {
                    loadout.ListenTo(HeroLoadout.Events.ItemInLoadoutChanged, OnItemInLoadoutChanged, this);
                }
            }
        }
        
        void OnLoadoutChanged(Change<int> loadoutIndexChange) {
            var heroItems = Target.Hero.HeroItems;
            
            var oldLoadout = heroItems.LoadoutAt(loadoutIndexChange.from);
            var newLoadout = heroItems.LoadoutAt(loadoutIndexChange.to);
            
            foreach (var s in EquipmentSlotType.Hands) {
                if (oldLoadout[s]?.Template != newLoadout[s]?.Template) {
                    _equipmentChanged = true;
                    return;
                }
            }
        }

        void OnItemInLoadoutChanged(HeroLoadout.LoadoutItemChange itemChange) {
            if (itemChange.to == null || !itemChange.to.Template.IsEquippable) {
                return;
            }
            
            var heroItems = Target.Hero.HeroItems;
            if (itemChange.loadout != heroItems?.CurrentLoadout) {
                return;
            }

            _equipmentChanged = true;
        }

        protected override void AfterBodyInstanceInitialized(GameObject instance) {
            _rotatable = instance.GetOrAddComponent<RotatableObject>();
            NewTarget.SetupRotatableArea(_rotatable);
        }
        
        // === LifeCycle
        protected override void OnUpdate() {
            if (_equipmentChanged) {
                RequestAnimationsForCurrentLoadout();
                _equipmentChanged = false;
            }
        }
        
        public void SetExternalHeroVisibility(bool visible) {
            SetViewTargetInstantWithoutChangeCurrent(visible ? _currentTarget : HeroRenderer.Target.OutOfScreen);
            SetRotatableState(visible);
            
            if (visible) {
                InitializeAnimationPlayback();
            }
        }

        public void SetViewTarget(HeroRenderer.Target viewTarget, bool allowRotation = true) {
            if (_currentTarget != viewTarget) {
                _currentTarget = viewTarget;
                _cameraSequence.Kill();
                var target = viewTargets.Get(viewTarget);

                _cameraSequence = DOTween.Sequence()
                    .Append(DOTween.To(() => cameraTransform.position, position => cameraTransform.position = position, target.position, cameraTransitionTime))
                    .Join(DOTween.To(() => cameraTransform.rotation, rotation => cameraTransform.rotation = rotation, target.rotation.eulerAngles, cameraTransitionTime))
                    .SetEase(Ease.InOutQuad)
                    .SetUpdate(true);
            }

            if (_rotatable != null && !allowRotation) {
                _rotatable.transform.DOLocalRotate(Vector3.zero, 0.5f).SetUpdate(true);
            }
        }
        
        public void SetViewTargetInstant(HeroRenderer.Target viewTarget) {
            if (_currentTarget != viewTarget) {
                _currentTarget = viewTarget;
                SetViewTargetInstant(viewTargets.Get(viewTarget));
            }
        }

        public void SetViewTargetInstantWithoutChangeCurrent(HeroRenderer.Target viewTarget) {
            SetViewTargetInstant(viewTargets.Get(viewTarget));
        }
        
        void SetViewTargetInstant(Transform target) {
            _cameraSequence.Kill();
            cameraTransform.position = target.position;
            cameraTransform.rotation = target.rotation;
        }
        
        public void SetRotatableState(bool state) {
            if (_rotatable != null) {
                _rotatable.SetCanRotate(state);
            }
        }

        public void SetViewTarget(EquipmentSlotType equipmentSlotType) {
            if (equipmentSlotType == EquipmentSlotType.Helmet) {
                SetViewTarget(HeroRenderer.Target.Head);
            } else if (equipmentSlotType == EquipmentSlotType.Greaves) {
                SetViewTarget(HeroRenderer.Target.Legs);
            } else if (equipmentSlotType == EquipmentSlotType.Gauntlets) {
                SetViewTarget(HeroRenderer.Target.Hand);
            } else if (equipmentSlotType == EquipmentSlotType.Boots) {
                SetViewTarget(HeroRenderer.Target.Feet);
            } else if (equipmentSlotType == EquipmentSlotType.Cuirass) {
                SetViewTarget(HeroRenderer.Target.Chest);
            } else if (equipmentSlotType == EquipmentSlotType.Back) {
                SetViewTarget(HeroRenderer.Target.Back, false);
            } else {
                SetViewTarget(HeroRenderer.Target.HeroUIInventory);
            }
        }
        
        public void ShowForegroundQuad() {
            foregroundQuadMaterial.SetColor(BaseColor, new Color(0, 0, 0, 1f));
        }
        
        public void HideForegroundQuad() {
            foregroundQuadMaterial.SetColor(BaseColor, Color.clear);
        }

        public void FadeForegroundQuad(float targetAlpha, float fadeTime, float fadeDelay) {
            _fadeTween.Kill();
            _fadeTween = foregroundQuadMaterial
                .DOColor(new Color(0f, 0f, 0f, targetAlpha), BaseColor, fadeTime)
                .SetUpdate(true).SetDelay(fadeDelay).SetEase(Ease.OutCubic);
        }

        protected override IBackgroundTask OnDiscard() {
            _fadeTween.Kill();
            HideForegroundQuad();
            return base.OnDiscard();
        }

        [Serializable]
        struct ViewTargets {
            [SerializeField] Transform hero;
            [SerializeField] Transform head;
            [SerializeField] Transform hand;
            [SerializeField] Transform legs;
            [SerializeField] Transform feet;
            [SerializeField] Transform chest;
            [SerializeField] Transform back;
            [SerializeField] Transform CCBody;
            [SerializeField] Transform CCHead;
            [SerializeField] Transform heroUIInventory;
            [SerializeField] Transform heroUIStatus;
            [SerializeField] Transform heroUIStatsSummary;
            [SerializeField] Transform outOfScreen;

            public Transform Get(HeroRenderer.Target target) {
                return target switch {
                    HeroRenderer.Target.Hero => hero,
                    HeroRenderer.Target.Head => head,
                    HeroRenderer.Target.Hand => hand,
                    HeroRenderer.Target.Legs => legs,
                    HeroRenderer.Target.Feet => feet,
                    HeroRenderer.Target.Chest => chest,
                    HeroRenderer.Target.Back => back,
                    HeroRenderer.Target.CCBody => CCBody,
                    HeroRenderer.Target.CCHead => CCHead,
                    HeroRenderer.Target.HeroUIInventory => heroUIInventory,
                    HeroRenderer.Target.HeroUIStatus => heroUIStatus,
                    HeroRenderer.Target.HeroUIStatsSummary => heroUIStatsSummary,
                    HeroRenderer.Target.OutOfScreen => outOfScreen,
                    _ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
                };
            }
        }
    }
}
