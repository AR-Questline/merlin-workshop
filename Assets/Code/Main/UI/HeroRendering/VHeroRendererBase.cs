using System;
using Animancer;
using Awaken.Kandra.AnimationPostProcessing;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.Cutscenes;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Character.Features.Config;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Animations;
using Awaken.TG.Main.Locations.Mobs;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Utility.Animations.HeroRenderer;
using Awaken.TG.MVC;
using Awaken.Utility.Animations;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.UI.HeroRendering {
    public abstract class VHeroRendererBase<T> : VHeroRendererBase where T : HeroRendererBase {
        public T NewTarget => Target as T;
    }

    public abstract class VHeroRendererBase : View<HeroRendererBase> {
        // === Serialized Fields
        [SerializeField] public GameObject characterPlacement;
        [Space(10f)] 
        [SerializeField, ARAssetReferenceSettings(new[] { typeof(HeroRendererAnimationMapping) })] 
        ARAssetReference maleAnimationMapping;
        [SerializeField, ARAssetReferenceSettings(new[] { typeof(HeroRendererAnimationMapping) })] 
        ARAssetReference femaleAnimationMapping;
        [SerializeField] ClipTransition maleDefaultAnimation;
        [SerializeField] ClipTransition femaleDefaultAnimation;
        [Space(10f)]
        [SerializeField, PrefabAssetReference(AddressableGroup.NPCs)] ARAssetReference malePrefab;
        [SerializeField, PrefabAssetReference(AddressableGroup.NPCs)] ARAssetReference femalePrefab;

        
        // === Fields
        ARAssetReference _bodyReference;
        GameObject _body;
        AnimationPostProcessing _animPP;
        AnimationPostProcessingPreset _animPPNoLeftArm;
        protected AnimancerComponent _animancer;

        ARAssetReference _animationsReference;
        HeroRendererAnimationMapping _animations;
        
        ClipTransition _defaultAnimation;
        
        // === Properties
        public bool IsLoading { get; private set; }
        protected abstract int BodyInstanceLayer { get; }
        public Transform HeadSocket { get; protected set; }
        public Transform MainHandSocket { get; protected set; }
        public Transform MainHandWristSocket { get; protected set; }
        public Transform OffHandSocket { get; protected set; }
        public Transform OffHandWristSocket { get; protected set; }
        public Transform RootSocket => BodyInstance?.transform;
        
        protected bool BodyLoaded { get; private set; }
        protected GameObject BodyInstance => _body;
        protected HeroRendererAnimationMapping Animations => _animations;
        
        protected HeroRendererAnimationEntry CurrentAnimatorEntry { get; private set; }
        protected HeroRendererAnimationEntry NextAnimatorEntry { get; private set; }
        protected AnimatorState CurrentAnimatorState  { get; private set; }
        protected AnimancerState CurrentClipState { get; private set; }
        
        // === LifeCycle
        protected override void OnInitialize() {
            _animPPNoLeftArm = CommonReferences.Get.noLeftArmPP;
        }

        void SetInternalHeroVisibility(bool visible) {
            characterPlacement.SetActive(visible);
        }
        
        void Update() {
            if (!BodyLoaded) {
                return;
            }

            OnUpdate();
            
            if (NextAnimatorEntry != null) {
                SetAnimatorState(AnimatorState.Equip);
            }

            if (CurrentClipState is { NormalizedTime: >= 1.0f }) {
                if (CurrentAnimatorState == AnimatorState.Equip) {
                    SetAnimatorState(AnimatorState.Idle);
                }
            }
        }
        protected virtual void OnUpdate() {}
        
        // === Body Instance
        public void HideBody() {
            SetInternalHeroVisibility(false);
        }
        
        public async UniTask ReloadBody() {
            SetInternalHeroVisibility(false);
            // TODO: wait for new full loading before unloading
            UnloadBodyInstance();
            IsLoading = true;
            try {
                await InitializeBodyInstance();

                if (_body == null) {
                    return;
                }

                var loadAnimationsTask = InitializeAnimations();
                var loadBodyFeaturesTask = LoadAllBodyFeatures();

                await loadAnimationsTask;
                await loadBodyFeaturesTask;
            } finally {
                IsLoading = false;
            }
        }
        
        public void ShowBody() {
            if (BodyInstance == null) {
                return;
            }
            
            BodyInstance.SetActive(true);
            SetInternalHeroVisibility(true);
            BodyLoaded = true;

            InitializeAnimationPlayback();
        }

        async UniTask InitializeBodyInstance() {
            _bodyReference = Target.GetGender() == Gender.Female ? femalePrefab : malePrefab;
            var prefab = await _bodyReference.LoadAsset<GameObject>();
            if (prefab == null) {
                Log.Important?.Error($"Cannot load body prefab with address: {_bodyReference.Address}");
                return;
            }

            _body = Object.Instantiate(prefab, characterPlacement.transform);
            _body.SetActive(false);
            
            FindTransformsInBodyInstance();
            AfterBodyInstanceInitialized(_body);
        }

        async UniTask LoadAllBodyFeatures() {
            var features = Target.BodyFeatures();

            var loadCustomClothesTask = LoadCustomHeroClothes(features);
            var loadPrefabClothes = BodyInstance.GetComponentInChildren<CharacterDefaultClothes>(true) is not { } oldClothes 
                ? UniTask.CompletedTask 
                : oldClothes.AddTo(features, true);

            await loadCustomClothesTask;
            await loadPrefabClothes;
            
            if (BodyInstance) {
                await features.ShowTask();
            }
        }
        
        UniTask LoadCustomHeroClothes(BodyFeatures features) {
            var heroClothes = Target.AddElement(new CustomHeroClothes());
            AttachHeroClothesListeners(heroClothes);
            features.InitCovers(heroClothes);
            return heroClothes.LoadEquipped();
        }

        void AttachHeroClothesListeners(CustomHeroClothes heroClothes) {
            heroClothes.ListenTo(BaseClothes.Events.ClothEquipped, OnVisualInstanceAttachedToBody, heroClothes);
            heroClothes.ListenTo(CustomHeroClothes.Events.WeaponEquipped, OnVisualInstanceAttachedToBody, heroClothes);
        }

        void OnVisualInstanceAttachedToBody(GameObject instance) {
            VFXManualSimulator.AttachTo(instance, VFXManualSimulator.UpdateMode.DeltaTimeDifference);
        }
        
        void FindTransformsInBodyInstance() {
            HeadSocket = null;
            MainHandSocket = null;
            OffHandSocket = null;
            MainHandWristSocket = null;
            OffHandWristSocket = null;
            
            Transform[] transforms = _body.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms) {
                var go = t.gameObject;
                go.layer = BodyInstanceLayer;
                if (go.CompareTag("MainHand")) {
                    MainHandSocket = t;
                } else if (go.CompareTag("OffHand")) {
                    OffHandSocket = t;
                } else if (go.CompareTag("Head")) {
                    HeadSocket = t;
                } else if (go.CompareTag("MainHandWrist")) {
                    MainHandWristSocket = t;
                } else if (go.CompareTag("OffHandWrist")) {
                    OffHandWristSocket = t;
                }
            }

            _animPP = _body.GetComponentInChildren<AnimationPostProcessing>();
            if (_animPP) {
                if (Target.Hero.HasElement<HeroOffHandCutOff>()) {
                    _animPP.ChangeAdditionalEntries(new[]{ new AnimationPostProcessing.Entry(_animPPNoLeftArm) });
                }
            } else {
                Log.Critical?.Error("No AnimationPostProcessing found in HeroRenderer prefab.", _body);
            }
        }
        
        protected virtual void AfterBodyInstanceInitialized(GameObject instance) {}

        // === Animations
        protected async UniTask InitializeAnimations() {
            _animancer = BodyInstance.GetComponentInChildren<AnimancerComponent>();
            var gender = Target.GetGender();
            _defaultAnimation = gender == Gender.Female ? femaleDefaultAnimation : maleDefaultAnimation;
            
            if (Target.UseLoadoutAnimations) {
                _animationsReference = gender == Gender.Female ? femaleAnimationMapping : maleAnimationMapping;
                _animations = await _animationsReference.LoadAsset<HeroRendererAnimationMapping>();
                if (_animations == null) {
                    Log.Important?.Error($"Loading loadout animations asset for HeroRenderer failed.");
                }
            }
        }
        
        protected void InitializeAnimationPlayback() {
            CurrentClipState = null;
            CurrentAnimatorEntry = null;
            NextAnimatorEntry = null;
            CurrentAnimatorState = AnimatorState.Idle;
            
            if (Target.UseLoadoutAnimations) {
                RequestAnimationsForCurrentLoadout();
            } else {
                PlayDefaultAnimation();
            }
        }
        
        void PlayDefaultAnimation() {
            CurrentAnimatorState = AnimatorState.Idle;
            PlayClip(_defaultAnimation);
        }

        protected void RequestAnimationsForCurrentLoadout() {
            if (!BodyLoaded) {
                return;
            }
            RequestAnimations(GetAnimationEntryForCurrentLoadout());
        }
        
        HeroRendererAnimationEntry GetAnimationEntryForCurrentLoadout() {
            var loadout = Target.Hero.HeroItems.CurrentLoadout;
            return Animations.FindFor(loadout);
        }
        
        void RequestAnimations(HeroRendererAnimationEntry entry) {
            if (CurrentAnimatorEntry != entry || CurrentAnimatorState == AnimatorState.Idle) {
                NextAnimatorEntry = entry;
            }
        }

        protected void SetAnimatorState(AnimatorState state) {
            if (state == AnimatorState.Equip) {
                if (NextAnimatorEntry != null) {
                    CurrentAnimatorEntry = NextAnimatorEntry;
                    NextAnimatorEntry = null;
                }

                if (!TryPlayClipForState(AnimatorState.Equip)) {
                    state = AnimatorState.Idle;
                }
            }

            if (state == AnimatorState.Idle) {
                if (!TryPlayClipForState(AnimatorState.Idle)) {
                    Log.Minor?.Warning($"HeroRenderer left with no loop animation.");
                }
            }

            CurrentAnimatorState = state;
        }
        
        bool TryPlayClipForState(AnimatorState state) {
            if (CurrentAnimatorEntry == null) return false;

            var clipStateToPlay = state switch {
                AnimatorState.Equip => CurrentAnimatorEntry.start,
                AnimatorState.Idle => CurrentAnimatorEntry.loop,
                _ => CurrentAnimatorEntry.loop,
            };

            bool canPlay = clipStateToPlay != null && clipStateToPlay.Clip != null;

            if (canPlay) {
                PlayClip(clipStateToPlay);
            }
            
            return canPlay;
        }

        void PlayClip(ClipTransition clip) {
            var fadeDuration = clip.FadeDuration;
            CurrentClipState = _animancer.Play(clip, fadeDuration, FadeMode.FromStart);
        }
        
        // === Discarding
        protected override IBackgroundTask OnDiscard() {
            UnloadBodyInstance();
            return base.OnDiscard();
        }
        
        protected void UnloadBodyInstance() {
            if (IsLoading) {
                Log.Critical?.Error("Trying to unload body instance while still loading.");
            }
            BodyLoaded = false;

            var features = Target.BodyFeatures();
            RemoveClothesSpawnedFromPrefab(features);
            features.Hide();
            Target.RemoveElementsOfType<CustomHeroClothes>();
            
            if (_bodyReference != null) {
                if (_body) {
                    Object.Destroy(_body);
                }
                _body = null;
                _animPP = null;
                _animancer = null;
                _bodyReference.ReleaseAsset();
                _bodyReference = null;
            }
            
            if (_animationsReference != null) {
                _animationsReference.ReleaseAsset();
                _animations = null;
                _animationsReference = null;
            }
        }
        
        public void RemoveClothesSpawnedFromPrefab(BodyFeatures features) {
            if (BodyInstance && BodyInstance.GetComponentInChildren<CharacterDefaultClothes>(true) is { } newClothes) {
                newClothes.RemoveFrom(features);
            }
        }

        // === Helpers
        [Serializable]
        public enum AnimatorState : byte {
            Idle,
            Equip,
        }
    }
}