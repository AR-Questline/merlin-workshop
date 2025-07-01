using Awaken.Utility.UI;
using Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.Utility.Debugging {
    public sealed class CameraDebugWindow : UGUIWindowDisplay<CameraDebugWindow> {
        bool _expandedLayers;
        float _maxQueuedFrames;

        protected override bool WithSearch => false;

        protected override void Initialize() {
            base.Initialize();
            _maxQueuedFrames = QualitySettings.maxQueuedFrames;
        }

        protected override void DrawWindow() {
            var camera = Camera.main;
            if (!camera) {
                GUILayout.Label("No main camera!");
                return;
            }

            var cameraBrain = camera.GetComponent<CinemachineBrain>();

            GUILayout.BeginHorizontal();
            var maxQueuedFrames = QualitySettings.maxQueuedFrames;
            maxQueuedFrames = TGGUILayout.DelayedIntField("Max queued frames", maxQueuedFrames);
            if (maxQueuedFrames != math.round(_maxQueuedFrames)) {
                _maxQueuedFrames = maxQueuedFrames;
            }
            _maxQueuedFrames = GUILayout.HorizontalSlider(_maxQueuedFrames, 0, 3);
            QualitySettings.maxQueuedFrames = (int)math.round(_maxQueuedFrames);
            GUILayout.EndHorizontal();

            camera.useOcclusionCulling = GUILayout.Toggle(camera.useOcclusionCulling, "Use occlusion culling");

            GUILayout.BeginHorizontal();
            var farClipPlane = camera.farClipPlane;
            farClipPlane = TGGUILayout.DelayedFloatField("Far plane", farClipPlane);
            farClipPlane = GUILayout.HorizontalSlider(farClipPlane, 50, 10000);
            camera.farClipPlane = farClipPlane;
            if (cameraBrain && cameraBrain.ActiveVirtualCamera is CinemachineVirtualCamera vc1) {
                var lens = vc1.m_Lens;
                lens.FarClipPlane = farClipPlane;
                vc1.m_Lens = lens;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            var nearClipPlane = camera.nearClipPlane;
            nearClipPlane = TGGUILayout.DelayedFloatField("Near plane", nearClipPlane);
            nearClipPlane = GUILayout.HorizontalSlider(nearClipPlane, 0.0001f, 10);
            camera.nearClipPlane = nearClipPlane;
            if (cameraBrain && cameraBrain.ActiveVirtualCamera is CinemachineVirtualCamera vc2) {
                var lens = vc2.m_Lens;
                lens.NearClipPlane = nearClipPlane;
                vc2.m_Lens = lens;
            }
            GUILayout.EndHorizontal();

            camera.layerCullSpherical = GUILayout.Toggle(camera.layerCullSpherical, "Layer cull spherical");
            _expandedLayers = TGGUILayout.Foldout(_expandedLayers, "Layer cull distances");
            if (_expandedLayers) {
                var layerCullDistances = camera.layerCullDistances;
                var changeScope = new TGGUILayout.CheckChangeScope();
                for (int i = 0; i < layerCullDistances.Length; i++) {
                    var layer = LayerMask.LayerToName(i);
                    GUILayout.BeginHorizontal();
                    var layerDistance = layerCullDistances[i];
                    layerDistance = TGGUILayout.DelayedFloatField(layer, layerDistance);
                    layerDistance = GUILayout.HorizontalSlider(layerDistance, 0, 10000);
                    layerCullDistances[i] = layerDistance;
                    GUILayout.EndHorizontal();
                }

                if (changeScope) {
                    camera.layerCullDistances = layerCullDistances;
                }
                changeScope.Dispose();
            }

            GUILayout.Space(4);
            var additionalData = camera.GetComponent<HDAdditionalCameraData>();
            if (!additionalData) {
                GUILayout.Label("Camera don't have HDAdditionalCameraData!");
                return;
            }
            additionalData.stopNaNs = GUILayout.Toggle(additionalData.stopNaNs, "Stop NaNs");
        }

        [StaticMarvinButton(state: nameof(IsDebugWindowShown))]
        static void ShowCameraDebugWindow() {
            CameraDebugWindow.Toggle(UGUIWindowUtils.WindowPosition.BottomLeft);
        }

        bool IsDebugWindowShown() => CameraDebugWindow.IsShown;
    }
}