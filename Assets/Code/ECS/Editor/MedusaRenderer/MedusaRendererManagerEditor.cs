using Awaken.ECS.MedusaRenderer;
using Awaken.Utility.Editor;
using Sirenix.OdinInspector.Editor;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Awaken.ECS.Editor.MedusaRenderer {
    [CustomEditor(typeof(MedusaRendererManager))]
    public class MedusaRendererManagerEditor : OdinEditor {
        float _gizmosDistance = 100f;
        int _gizmosFor = -1;

        protected override void OnEnable() {
            base.OnEnable();
            EditorApplication.update -= Repaint;
            EditorApplication.update += Repaint;
        }

        protected override void OnDisable() {
            EditorApplication.update -= Repaint;
            base.OnDisable();
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if (!Application.isPlaying) {
                return;
            }

            var manager = (MedusaRendererManager)target;
            var access = new MedusaRendererManager.EditorAccess(manager);

            if (!access.BrgRenderer.IsValid) {
                return;
            }

            _gizmosDistance = EditorGUILayout.FloatField("Gizmos distance", _gizmosDistance);
            _gizmosFor = EditorGUILayout.IntSlider("Gizmos for", _gizmosFor, -1, (int)access.BrgRenderer.Xs.Length - 1);
        }

        void OnSceneGUI() {
            if (!Application.isPlaying) {
                return;
            }
            var manager = (MedusaRendererManager)target;
            var access = new MedusaRendererManager.EditorAccess(manager);
            var rendererAccess = access.BrgRenderer;

            if (!rendererAccess.IsValid) {
                return;
            }

            var camera = SceneView.currentDrawingSceneView.camera;
            var cameraData = new float3x2(camera.transform.position, camera.transform.forward);

            for (var i = 0u; i < rendererAccess.Xs.Length; i++) {
                if (_gizmosFor != -1 && i != _gizmosFor) {
                    continue;
                }
                var position = new float3(rendererAccess.Xs[i], rendererAccess.Ys[i], rendererAccess.Zs[i]);
                var radius = rendererAccess.Radii[i];

                if (math.distancesq(cameraData.c0, position) >= _gizmosDistance * _gizmosDistance) {
                    continue;
                }

                HandlesUtils.DrawSphere(position, radius);

                var label = $"{i}. Splitmask {rendererAccess.SplitVisibilityMask[i]}\nLod {rendererAccess.LodVisibility[i]}";
                HandlesUtils.Label(position, label, Color.white, EditorStyles.label, out _, cameraData);
            }
        }
    }
}
