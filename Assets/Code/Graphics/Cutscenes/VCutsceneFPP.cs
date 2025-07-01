using Awaken.TG.Assets;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.Utility.Animations;
using Cinemachine;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Graphics.Cutscenes {
    public class VCutsceneFPP : VCutsceneBase {
        // === Fields
        [SerializeField] AnimationClip animationClip;
        [Tooltip("If it's set to 0 animation will start after toCameraDuration"), SerializeField] float animationStartDelayOverride = 0;

        ARAsyncOperationHandle<GameObject> _bodyInstanceHandle;
        ARNpcAnimancer _animator;
        
        // === Properties
        public Transform HeadSocket { get; private set; }
        public Transform MainHandSocket { get; private set; }
        public Transform OffHandSocket { get; private set; }

        protected override Transform TeleportHeroTo => RootSocket;
        protected override float ToCameraAwaitDuration => animationStartDelayOverride == 0 ? toCameraDuration : animationStartDelayOverride;
        ShareableARAssetReference MalePrefab => Services.Get<CommonReferences>().maleCutscenePrefab;
        ShareableARAssetReference FemalePrefab => Services.Get<CommonReferences>().femaleCutscenePrefab;

        protected override async UniTaskVoid Load() {
            BodyFeatures features = Target.Element<BodyFeatures>();
            var prefab = features.Gender == Gender.Female ? FemalePrefab : MalePrefab;
            _bodyInstanceHandle = prefab.Get().LoadAsset<GameObject>();
            GameObject result = await _bodyInstanceHandle;
            if (result == null) {
                ReleaseBodyInstance();
                return;
            }
            
            if (HasBeenDiscarded) {
                ReleaseBodyInstance();
                return;
            }

            GameObject instance = Object.Instantiate(result, transform);
            RootSocket = instance.transform;
            if (gameObject == null || HasBeenDiscarded) return;

            var heroClothes = Target.AddElement(new CustomHeroClothes(false));
            features.InitCovers(heroClothes);
            await heroClothes.LoadEquipped();
            if (gameObject == null || HasBeenDiscarded) return;
            
            await features.ShowTask();
            if (gameObject == null || HasBeenDiscarded) return;
            
            HeadSocket = null;
            MainHandSocket = null;
            OffHandSocket = null;
            
            Transform[] transforms = instance.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms) {
                var go = t.gameObject;
                if (go.CompareTag("MainHand")) {
                    MainHandSocket = t;
                } else if (go.CompareTag("OffHand")) {
                    OffHandSocket = t;
                } else if (go.CompareTag("Head")) {
                    HeadSocket = t;
                }
            }

            CutsceneCamera = instance.GetComponentInChildren<CinemachineVirtualCamera>();
            _animator = instance.GetComponentInChildren<ARNpcAnimancer>();
            _animator.Play(animationClip);
            _animator.Evaluate(0);
            Target.GetOrCreateTimeDependent().WithUpdate(ProcessUpdate).ThatProcessWhenPause();
            
            StartTransition().Forget();
        }
        
        // === Playing Animation
        protected override void ProcessUpdate(float deltaTime) {
            if (!_isPlaying) {
                _animator.Playable.PauseGraph();
                return;
            }
            
            _timeElapsed += pauseGame ? Time.unscaledDeltaTime : deltaTime;
            if (pauseGame) {
                _animator.Evaluate(Time.unscaledDeltaTime);
            } else {
                _animator.Playable.UnpauseGraph();
            }
            if (_timeElapsed >= animationClip.length) {
                StopTransition().Forget();
            }
        }

        // === Creation
        public static VCutsceneFPP CreateNewPrefab(string name) {
            var cutsceneGameObject = new GameObject(name, typeof(VCutsceneFPP));
            var vCutscene = cutsceneGameObject.GetComponent<VCutsceneFPP>();
            return vCutscene;
        }
        
        // === Skipping
        protected override IBackgroundTask OnDiscard() {
            ReleaseBodyInstance();
            return base.OnDiscard();
        }

        void ReleaseBodyInstance() {
            if (_bodyInstanceHandle.IsValid()) {
                _bodyInstanceHandle.Release();
                _bodyInstanceHandle = default;
            }
        }
    }
}