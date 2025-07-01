using System.Linq;
using System.Text;
using Awaken.TG.Main.Cameras.CameraStack;
using Awaken.TG.Main.Cameras.Controllers;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using CameraState = Awaken.TG.Main.Cameras.CameraStack.CameraState;

namespace Awaken.TG.Main.Cameras {
    /// <summary>
    /// Represents the in-game camera. The model allows for focusing on specific objects
    /// on the game map and free camera movement under the player's control.
    /// </summary>
    [SpawnsView(typeof(VGameCamera))]
    public partial class GameCamera : Model {
        public override Domain DefaultDomain => Domain.Globals;
        public sealed override bool IsNotSaved => true;

        // === Cache
        public static readonly int ZoomShaderID = Shader.PropertyToID("_CameraZoom");
        
        // === State
        WeakModelRef<IFollowTarget> _followTarget;
        
        public ControlType Control { get; private set; } = ControlType.TPP;
        public Camera MainCamera {
            get {
                if (_mainCamera == null) {
                    _mainCamera = Camera.main;
                    if (_mainCamera == null) {
                        var cameras = Object.FindObjectsByType<Camera>(
                            FindObjectsInactive.Include, FindObjectsSortMode.None);
                        _mainCamera = cameras.FirstOrDefault(x => x.gameObject.CompareTag("MainCamera"));
                    }
                }
                return _mainCamera;
            }
        }

        public CinemachineVirtualCamera CinemachineVirtualCamera { get; private set; }
        public CameraShaker Shaker { get; private set; }

        Camera _mainCamera;
        ScreenShakesReactiveSetting _screenShakesReactiveSetting;
        ScreenShakesProactiveSetting _screenShakesProactiveSetting;
        Sequence _sequence;

        // === Events
        public new static class Events {
            public static readonly Event<GameCamera, ControlType> ControlChanged = new(nameof(ControlChanged));
        }

        // === Initialization
        protected override string GenerateID(Services services, StringBuilder idBuilder) => "GameCamera";

        protected override void OnInitialize() {
            World.Only<CameraStateStack>().PushCamera(new CameraState(MainCamera, this));
        }

        // === Camera Control
        public void ChangeControlType(ControlType newType) {
            if (Control != newType) {
                Control = newType;
                this.Trigger(Events.ControlChanged, newType);
            }
        }

        public async UniTask Shake(bool isProactiveAction, float amplitude = 0.5f, float frequency = 0.15f, float time = 0.5f, float pick = 0.1f) {
            _screenShakesReactiveSetting ??= World.Only<ScreenShakesReactiveSetting>();
            _screenShakesProactiveSetting ??= World.Only<ScreenShakesProactiveSetting>();
            bool shakesEnabled = isProactiveAction ? _screenShakesProactiveSetting.Enabled : _screenShakesReactiveSetting.Enabled;

            if (Shaker != null && shakesEnabled) {
                await Shaker.Shake(amplitude, frequency, time, pick);
            }
        }
        
        public void SetIgnoreTimescale(bool ignore) {
            if (_mainCamera != null) {
                _mainCamera.GetComponent<CinemachineBrain>().m_IgnoreTimeScale = ignore;
            }
        }

        // === Manual control

        public void TakeManualControl() {
            if (Control != ControlType.None) {
                ChangeControlType(ControlType.None);
            }
        }

        public void ReleaseManualControl() {
            if (Control == ControlType.None) {
                ChangeControlType(_followTarget.Get() != null ? ControlType.TPP : ControlType.None);
            }
        }
        
        // === Helpers

        public void SetCinemachineCamera(CinemachineVirtualCamera camera) {
            CinemachineVirtualCamera = camera;
            Shaker = camera.GetComponent<CameraShaker>();
        }

        public void ResetCinemachineCamera() {
            CinemachineVirtualCamera = null;
            Shaker = null;
        }

        public void RestoreDefaultPhysicalProperties() {
            // --- Camera body
            MainCamera.sensorSize = new Vector2(36, 24);
            MainCamera.iso = 200;
            MainCamera.shutterSpeed = 0.005f;
            MainCamera.gateFit = Camera.GateFitMode.Horizontal;
            // --- Lens
            MainCamera.focalLength = 17.13778f;
            MainCamera.lensShift = Vector2.zero;
            MainCamera.aperture = 16;
            MainCamera.focusDistance = 10;
            // --- Aperture Shape
            MainCamera.bladeCount = 5;
            MainCamera.curvature = new Vector2(2, 11);
            MainCamera.barrelClipping = 0.25f;
            MainCamera.anamorphism = 0;
        }
    }
}