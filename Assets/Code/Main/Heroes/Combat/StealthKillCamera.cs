using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Cinemachine;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public partial class StealthKillCamera : Element<HeroCameraShakes>, IWithDuration {
        public sealed override bool IsNotSaved => true;

        readonly Vector3 _spawnPosition;
        readonly Quaternion _spawnRotation;
        readonly Transform _lookAt;
        readonly float _duration;
        
        GameObject _cameraInstance;
        ARAsyncOperationHandle<GameObject> _cameraHandle;
        
        public IModel TimeModel => ParentModel.ParentModel;
        static ShareableARAssetReference CameraPrefab => Services.Get<CommonReferences>().stealthKillCameraPrefab;

        public StealthKillCamera(Transform lookAt, Vector3 spawnPosition, Quaternion spawnRotation, float duration) {
            _lookAt = lookAt;
            _spawnPosition = spawnPosition;
            _spawnRotation = spawnRotation;
            _duration = duration;
        }

        protected override void OnInitialize() {
            SpawnStealthKillCamera().Forget();
        }
        
        protected override void OnFullyInitialized() {
            var timeDuration = AddElement(new TimeDuration(_duration, true));
            timeDuration.ListenTo(Events.AfterDiscarded, Discard, this);
        }

        async UniTaskVoid SpawnStealthKillCamera() {
            _cameraHandle = CameraPrefab.Get().LoadAsset<GameObject>();
            var result = await _cameraHandle;
            if (result == null || HasBeenDiscarded) {
                ReleaseCameraInstance();
                return;
            }

            _cameraInstance = Object.Instantiate(result, _spawnPosition, _spawnRotation);
            CinemachineVirtualCamera virtualCamera = _cameraInstance.GetComponent<CinemachineVirtualCamera>();
            virtualCamera.LookAt = _lookAt;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            ReleaseCameraInstance();
            base.OnDiscard(fromDomainDrop);
        }

        // === Helpers
        void ReleaseCameraInstance() {
            if (_cameraHandle.IsValid()) {
                _cameraHandle.Release();
                _cameraHandle = default;
            }

            if (_cameraInstance != null) {
                Object.Destroy(_cameraInstance);
                _cameraInstance = null;
            }
        }
    }
}