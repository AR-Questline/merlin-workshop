using Awaken.TG.Assets;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.Utility.Animations;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Graphics.Cutscenes {
    public class VCutsceneFreeCam : VCutsceneBase {
        // === Fields
        [SerializeField] Transform cameraParent;
        [SerializeField] float duration;
        [SerializeField] bool teleportToCameraOnEnd;
        [SerializeField, ARAssetReferenceSettings(new[] { typeof(GameObject) }, group: AddressableGroup.NPCs), HideIf(nameof(useExistingCameraFromParent))]
        ShareableARAssetReference cutsceneCameraPrefab;
        [SerializeField, HideIf(nameof(IsCustomCameraSet))] bool useExistingCameraFromParent;

        ARAsyncOperationHandle<GameObject> _cameraInstanceHandle;

        // === Properties
        protected override Transform TeleportHeroTo => teleportToCameraOnEnd ? cameraParent : null;

        ShareableARAssetReference CameraPrefab => IsCustomCameraSet
            ? cutsceneCameraPrefab
            : Services.Get<CommonReferences>().cameraCutscenePrefab;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().LocationsHost(Target.CurrentDomain);
        bool IsCustomCameraSet => (cutsceneCameraPrefab?.IsSet ?? false);
        
        
        // === Initialization
        protected override async UniTaskVoid Load() {
            if (useExistingCameraFromParent) {
                CutsceneCamera = cameraParent.GetComponentInChildren<CinemachineVirtualCamera>(true);
                if (CutsceneCamera == null) return;
                RootSocket = CutsceneCamera.transform;
            } else {
                _cameraInstanceHandle = CameraPrefab.Get().LoadAsset<GameObject>();
                GameObject result = await _cameraInstanceHandle;
                if (result == null) {
                    ReleaseCameraInstance();
                    return;
                }

                if (HasBeenDiscarded || gameObject == null) {
                    ReleaseCameraInstance();
                    return;
                }

                RootSocket = Object.Instantiate(result, cameraParent).transform;
                CutsceneCamera = RootSocket.GetComponentInChildren<CinemachineVirtualCamera>();
            }

            Target.GetOrCreateTimeDependent().WithUpdate(ProcessUpdate).ThatProcessWhenPause();
            
            StartTransition().Forget();
        }
        
        // === Playing Animation
        protected override void ProcessUpdate(float deltaTime) {
            if (!_isPlaying) return;
            _timeElapsed += pauseGame ? Time.unscaledDeltaTime : deltaTime;
            if (_timeElapsed >= duration) {
                StopTransition().Forget();
            }
        }

        // === Creation
        public static VCutsceneFreeCam CreateNewPrefab(string name) {
            var cutsceneGameObject = new GameObject(name, typeof(VCutsceneFreeCam));
            var vCutscene = cutsceneGameObject.GetComponent<VCutsceneFreeCam>();
            return vCutscene;
        }
        
        // === Skipping
        protected override IBackgroundTask OnDiscard() {
            ReleaseCameraInstance();
            return base.OnDiscard();
        }

        void ReleaseCameraInstance() {
            if (_cameraInstanceHandle.IsValid()) {
                _cameraInstanceHandle.Release();
                _cameraInstanceHandle = default;
            }
        }
    }
}