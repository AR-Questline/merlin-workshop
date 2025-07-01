using Animancer;
using Awaken.TG.Assets;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.UI.PhotoMode;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Sources;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.Maths;
using Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Random = UnityEngine.Random;

namespace Awaken.TG.Main.UI.HeroRendering {
    [UsesPrefab("UI/HeroRendering/VWorldHeroRenderer")]
    public class VWorldHeroRenderer : VHeroRendererBase<WorldHeroRenderer>, IUIAware {
        const float IdleTime = 30f;
        const float IdleRotationSpeed = 2f;
        const float FreeCamSpeed = 5;
        const float RotationSpeed = 5;
        const float GamepadRotationSpeed = 50;
        const float ZoomSensitivity = 1.75f;

        // === Fields
        [SerializeField] Transform cameraLookAt;
        [SerializeField] CinemachineVirtualCamera virtualCamera;
        [SerializeField] CinemachineVirtualCamera freeCamera;
        [SerializeField, ARAssetReferenceSettings(new [] { typeof(ARPhotoModeAnimations)}, group: AddressableGroup.Animations)] 
        ARAssetReference customAnimationClips;

        bool _uiEnabled = true;
        AnimationClip _currentAnimationClip;
        Cinemachine3rdPersonFollow _3RdPersonFollow;
        float _currentZoom;
        Vector3 _noClipMoveIntent = Vector3.zero;
        bool _noClipFast;
        ARAsyncOperationHandle<ARPhotoModeAnimations> _customAnimationsHandle;
        ARPhotoModeAnimations _customAnimations;
        float _idleTimer;
        bool _isIdling;
        int _rotationSign;

        // === Properties
        
        public int AnimationPosesCount => _customAnimations?.Count ?? 0;
        protected override int BodyInstanceLayer => RenderLayers.Default;
        
        // === LifeCycle
        protected override void OnInitialize() {
            base.OnInitialize();
            
            _idleTimer = IdleTime;
            World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.Keyboard, this, Target));
            transform.position = Hero.Current.Coords;
            transform.forward = Hero.Current.Forward();
            _3RdPersonFollow = virtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            _currentZoom = 1;
            UpdateZoom();
            LoadCustomAnimations();

            World.EventSystem.ListenTo(EventSelector.AnySource, PhotoModeUI.Events.NextPoseChanged, this, GetAnimationClip);
            World.EventSystem.ListenTo(EventSelector.AnySource, PhotoModeUI.Events.PreviousPoseChanged, this, GetAnimationClip);
            World.EventSystem.ListenTo(EventSelector.AnySource, PhotoModeUI.Events.UIToggled, this, ToggleUI);
            World.EventSystem.ListenTo(EventSelector.AnySource, PhotoModeUI.Events.CameraToggled, this, ToggleCamera);
        }
        
        protected override void OnUpdate() {
            // if (RewiredHelper.Player.GetAnyButton()) {
            //     _idleTimer = IdleTime;
            //     _isIdling = false;
            // }

            if (_idleTimer > 0) {
                _idleTimer -= Time.unscaledDeltaTime;
            } else if (!_isIdling) {
                _rotationSign = Random.value < 0.5f ? -1 : 1;
                _isIdling = true;
            }

            if (_isIdling) {
                UpdateVirtualCameraRotation(_rotationSign * IdleRotationSpeed);
                return;
            }
            
            bool enableCamera = RewiredHelper.IsGamepad 
                                || !_uiEnabled 
                                //|| RewiredHelper.Player.GetButton(KeyBindings.Gameplay.PhotoMode.EnableCameraMovement)
                                ;

            if (enableCamera) {
                UpdateVirtualCameraRotation();
                UpdateFreeCamRotation();
                UpdateFreeCamPosition();
            }
            
            UpdateVirtualCameraPosition();
        }

        void ToggleUI(bool enabled) {
            _uiEnabled = enabled;
        }

        void LoadCustomAnimations() {
            _customAnimationsHandle = customAnimationClips.LoadAsset<ARPhotoModeAnimations>();
            _customAnimationsHandle.OnComplete(result => {
                if (result.Status == AsyncOperationStatus.Succeeded) {
                    _customAnimations = result.Result;
                }
            });
        }

        float CameraRotationSpeed() {
            if (_isIdling) {
                return IdleRotationSpeed;
            }
            
            return RewiredHelper.IsGamepad ? GamepadRotationSpeed : RotationSpeed;
        }

        void ToggleCamera() {
            // freeCamera.gameObject.SetActive(!freeCamera.gameObject.activeSelf);
        }

        void GetAnimationClip(int poseIndex) {
            if (_customAnimations == null) {
                return;
            }

            _currentAnimationClip = _customAnimations[poseIndex];
            if (_currentAnimationClip == null) {
                SetAnimatorState(AnimatorState.Idle);
                RequestAnimationsForCurrentLoadout();
            } else {
                _animancer.Play(_currentAnimationClip, 0, FadeMode.FromStart);
            }
        }
        
        void UpdateVirtualCameraPosition() {
            if (!CinemachineCore.Instance.IsLive(virtualCamera)) {
                return;
            }

            // float zoomDelta = (RewiredHelper.IsGamepad
            //                       ? RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.PhotoMode.ZoomIn) - RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.PhotoMode.ZoomOut)
            //                       : RewiredHelper.Player.GetAxis(KeyBindings.UI.Generic.ScrollVertical))
            //                   * Time.unscaledDeltaTime * ZoomSensitivity;
            // if (math.abs(zoomDelta) > 0) {
            //     _currentZoom = math.clamp(_currentZoom - zoomDelta, 0, 1);
            //     UpdateZoom();
            // }
        }

        void UpdateVirtualCameraRotation() {
            //UpdateVirtualCameraRotation(RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.CameraHorizontal));
        }

        void UpdateVirtualCameraRotation(float deltaY) {
            if (!CinemachineCore.Instance.IsLive(virtualCamera)) {
                return;
            }
            
            float modifier = Time.unscaledDeltaTime * CameraRotationSpeed();
            float deltaX = 0;//RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.CameraVertical) * modifier;
            deltaY *= modifier;
            Vector3 localEuler = cameraLookAt.localEulerAngles;
            float x = GeneralUtils.ClampEulerAngle(localEuler.x + deltaX, -85, 85);
            cameraLookAt.localRotation = Quaternion.Euler(x, localEuler.y + deltaY, 0.0f);
        }

        void UpdateFreeCamPosition() {
            if (!CinemachineCore.Instance.IsLive(freeCamera)) {
                return;
            }
            Transform freeCameraTransform = freeCamera.transform;
            
            // === Move
            Vector2 moveVector = new(
                0,//RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.Horizontal), 
                0 //RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.Vertical)
            );
            Vector3 forward = freeCameraTransform.forward * moveVector.y;
            Vector3 right = freeCameraTransform.right * moveVector.x;
                
            Vector3 moveTowards = right + forward;
            Vector3 verticalVel = _noClipMoveIntent;
            _noClipMoveIntent = Vector3.zero;
            float speedMultiplier = Time.unscaledDeltaTime * FreeCamSpeed;
            if (_noClipFast) {
                speedMultiplier *= 2;
                _noClipFast = false;
            }
            freeCameraTransform.position += (moveTowards + verticalVel) * speedMultiplier;
        }
        
        void UpdateFreeCamRotation() {
            if (!CinemachineCore.Instance.IsLive(freeCamera)) {
                return;
            }
            
            Transform freeCameraTransform = freeCamera.transform;
            
            float deltaX = 0;//RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.CameraVertical) * Time.unscaledDeltaTime;
            float deltaY = 0;//RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.CameraHorizontal) * Time.unscaledDeltaTime;

            var freeCamEuler = freeCameraTransform.localEulerAngles;
            freeCamEuler.x += deltaX * CameraRotationSpeed();
            freeCamEuler.y += deltaY * CameraRotationSpeed();
            freeCamEuler.z = 0;
            freeCameraTransform.localEulerAngles = freeCamEuler;
        }

        void UpdateZoom() {
            _3RdPersonFollow.CameraDistance = Mathf.Lerp(0, 20, _currentZoom);
            _3RdPersonFollow.VerticalArmLength = Mathf.Lerp(-5, 0, _currentZoom);
        }

        public UIResult Handle(UIEvent evt) {
            bool cheatsEnabled = CheatController.CheatsEnabled();
            if (!cheatsEnabled) {
                return UIResult.Ignore;
            }
            
            if (evt is not UIKeyHeldAction keyHeld) return UIResult.Ignore;
            
            if (keyHeld.Name == KeyBindings.Debug.DebugNoClipAccelerate) {
                _noClipFast = true;
                return UIResult.Accept;
            }
            
            if (keyHeld.Name == KeyBindings.Debug.DebugNoClipUp) {
                _noClipMoveIntent = Vector3.up;
                return UIResult.Accept;
            }
            if (keyHeld.Name == KeyBindings.Debug.DebugNoClipDown) {
                _noClipMoveIntent = Vector3.down;
                return UIResult.Accept;
            }

            return UIResult.Ignore;
        }

        protected override IBackgroundTask OnDiscard() {
            _customAnimationsHandle.Release();
            _customAnimationsHandle = default;
            _customAnimations = null;
            return base.OnDiscard();
        }
    }
}