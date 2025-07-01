using System.Collections.Generic;
using System.Linq;
using Awaken.CommonInterfaces;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Cameras.CameraStack {
    public partial class CameraStateStack : Model {
        const int UiLayer = 5;

        public override Domain DefaultDomain => Domain.Globals;
        public sealed override bool IsNotSaved => true;

        // === State
        List<CameraState> _cameras = new List<CameraState>();
        List<CameraHandle> _cameraHolders = new List<CameraHandle>();

        CameraState _previousState;

        // === Properties
        public CameraHandle MainHandle { get; private set; }
        public CameraState CurrentState => _cameras.LastOrDefault(c => !c.Additive);
        public Camera MainCamera => MainHandle.Camera;

        // === Events
        public new static class Events {
            public static readonly Event<CameraStateStack, CameraState> CameraChanged = new(nameof(CameraChanged));
        }

        // === Initialization
        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, Model.Events.BeforeDiscarded, this, ReleaseAllOwnedBy);
            MainHandle = new CameraHandle();
            PushCameraHandle(MainHandle);
        }

        // == Operations
        public void PushCamera(CameraState cameraState) {
            _cameras.Add(cameraState);
            OnCameraChanged();
        }

        public void ReleaseAllOwnedBy(IModel owner) {
            _cameras.RemoveAll(c => c.Owner == owner);
            _cameraHolders.RemoveAll(h => h.Owner == owner);
            OnCameraChanged();
        }

        public void PushCameraHandle(CameraHandle handle) {
            _cameraHolders.Add(handle);
        }

        public static int CullAllOnMainCamera() {
            var mainCamera = World.Only<CameraStateStack>().MainCamera;
            var mask = mainCamera.cullingMask;
            mainCamera.cullingMask = 0;
            return mask;
        }

        public static void RestoreCullingMaskOnMainCamera(int mask) {
            World.Only<CameraStateStack>().MainCamera.cullingMask = mask;
        }

        public void DisableCameras() {
            foreach (var cameraState in _cameras) {
                cameraState.Camera.enabled = false;
            }
        }

        // === Helpers
        void OnCameraChanged() {
            if (CurrentState != _previousState) {
                this.Trigger(Events.CameraChanged, CurrentState);
                _previousState = CurrentState;
                
                // Activate last main camera and all following additive cameras.
                bool cameraEnabled = false;
                foreach (var cameraState in _cameras) {
                    if (cameraState == _previousState) {
                        cameraEnabled = true;
                    }
                    cameraState.Camera.enabled = cameraEnabled;
                }

                if (CurrentState != null) {
                    _cameraHolders.ForEach(h => h.ChangeCamera(CurrentState.Camera));
                }

                CurrentCamera.Value = MainCamera;
            }
        }

        static CameraHandle s_camCache;
        
        public static void EDITOR_RuntimeReset() {
            s_camCache = null;
        }

        /// <summary>
        /// Gets camera depending on current editor/game state
        /// </summary>
        public static bool TryGetCamera(out Camera cam) {
#if UNITY_EDITOR
            if (Application.isPlaying) {
                s_camCache ??= World.Any<CameraStateStack>()?.MainHandle;
                cam = s_camCache?.Camera;
            } else {
                if (UnityEditor.SceneView.lastActiveSceneView != null && UnityEditor.SceneView.lastActiveSceneView.camera != null) {
                    cam = UnityEditor.SceneView.lastActiveSceneView.camera;
                } else {
                    //No scene window open case while not playing, no camera to be found
                    cam = null;
                    return false;
                }
            }
            if (cam == null) {
                Log.Important?.Error("Camera Null");
            }
#else
            s_camCache ??= World.Any<CameraStateStack>()?.MainHandle;
            cam = s_camCache?.Camera;
#endif

            return cam != null;
        }
    }
}