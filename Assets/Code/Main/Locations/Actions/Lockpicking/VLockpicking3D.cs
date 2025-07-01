using Awaken.CommonInterfaces;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.DrakeRenderer.Systems;
using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.GameObjects;
using Cinemachine;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Lockpicking {
    public abstract class VLockpicking3D : View<LockpickingInteraction> {
        [SerializeField] RotationAdjuster pick;
        [SerializeField] RotationAdjuster lockRotator;
        [SerializeField] PositionAdjuster lockCylinder;

        [SerializeField] CinemachineVirtualCamera lockCamera;
        [SerializeField] Animator _animator;

        public Transform AnimationsParent => _animator.transform;

        void Awake() {
            var renderers = GetComponentsInChildren<IWithUnityRepresentation>();
            foreach (var unityRepresentation in renderers) {
                unityRepresentation.SetUnityRepresentation(new IWithUnityRepresentation.Options {
                    linkedLifetime = true,
                    movable = true
                });
            }

            var drakeLods = GetComponentsInChildren<DrakeLodGroup>();
            foreach (var drakeLod in drakeLods) {
                if (!drakeLod.gameObject.HasComponent<ForceDrakeLoad>()) {
                    drakeLod.gameObject.AddComponent<ForceDrakeLoad>();
                }
                if (!drakeLod.gameObject.HasComponent<ForceDrakeMipmapsFull>()) {
                    drakeLod.gameObject.AddComponent<ForceDrakeMipmapsFull>();
                }
            }

            DrakeRendererStateSystem.PushSystemFreeze();
            ++LoadingStates.PauseHlodUpdateCounter;
        }

        protected override void OnInitialize() {
            lockCamera.Priority = -1;
            Target.ListenTo(LockpickingInteraction.Events.LevelStarted, LayerChanged, Target);
        }

        void LateUpdate() {
            UpdateVisualState();
        }

        public void SetupCamera() {
            TeleportToCameraPosition().Forget();
        }
        
        async UniTaskVoid TeleportToCameraPosition() {
            var mainCamTransform = World.Only<GameCamera>().MainCamera.transform;
            transform.position = mainCamTransform.position;
            transform.forward = mainCamTransform.forward;
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            lockCamera.Priority = 9999;
        }

        void LayerChanged(int newLayer) {
            lockCylinder.SetPosition(newLayer);
        }

        void UpdateVisualState() {
            pick.SetRotation(Target.CurrentPicklockRotation);
            lockRotator.SetRotation(Target.CurrentLockRotation);
        }

        protected override IBackgroundTask OnDiscard() {
            lockCamera.Priority = -1;

            DrakeRendererStateSystem.PopSystemFreeze();
            --LoadingStates.PauseHlodUpdateCounter;

            return base.OnDiscard();
        }
    }
}
