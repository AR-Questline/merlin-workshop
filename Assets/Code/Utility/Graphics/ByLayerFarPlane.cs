using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.Utility.Graphics {
    [RequireComponent(typeof(Camera))]
    public class ByLayerFarPlane : MonoBehaviour {
        const int LayerCount = 32;
        
        [SerializeField, OnValueChanged(nameof(ApplyLayers), true)]
        LayerSetting[] settings = Array.Empty<LayerSetting>();
        
        Camera _camera;
        float[] _farPlanes = new float[LayerCount];

        void Start() {
            _camera = GetComponent<Camera>();
            ApplyLayers();
        }
        
        void ApplyLayers() {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                return;
            }
#endif
            Array.Fill(_farPlanes, 0);
            foreach (var setting in settings) {
                var layer = setting.layers;
                var farPlane = setting.farPlane;
                for (var i = 0; i < LayerCount; i++) {
                    if ((layer & (1 << i)) != 0) {
                        _farPlanes[i] = farPlane;
                    }
                }
            }
            
            _camera.layerCullDistances = _farPlanes;
        }
        
        [Serializable]
        struct LayerSetting {
            public LayerMask layers;
            [Range(0, 10000), Tooltip("Zero means camera far plane")]
            public float farPlane;
#if UNITY_EDITOR
            public Color gizmosColor;
#endif
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected() {
            var camera = _camera ?? GetComponent<Camera>();
            if (!camera) {
                return;
            }

            var oldGizmosMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            
            foreach (var setting in settings) {
                Gizmos.color = setting.gizmosColor;
                Gizmos.DrawFrustum(Vector3.zero, camera.fieldOfView, setting.farPlane, camera.nearClipPlane,
                    camera.aspect);
            }
            
            Gizmos.matrix = oldGizmosMatrix;
        }
#endif
    }
}
