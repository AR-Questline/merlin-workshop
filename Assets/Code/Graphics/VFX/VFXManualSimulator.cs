using System;
using Awaken.Kandra;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Graphics.VFX {
    public class VFXManualSimulator : MonoBehaviour {
        static readonly int UnscaledTimeId = Shader.PropertyToID("_UnscaledTime");
        
        public UpdateMode updateMode = UpdateMode.None;
        public float manualPlayRate = 1f;
        
        VisualEffect[] _visualEffects = Array.Empty<VisualEffect>();
        IVFXManuallySimulated[] _simulatedComponents = Array.Empty<IVFXManuallySimulated>();
        Material[] _materials = Array.Empty<Material>();
        KandraRenderer _kandraRenderer;
        float _currentTime;
        
        void Awake() {
            _visualEffects = GetComponentsInChildren<VisualEffect>();
            _simulatedComponents = GetComponentsInChildren<IVFXManuallySimulated>();
            _kandraRenderer = GetComponentInChildren<KandraRenderer>();
            if (_kandraRenderer != null) {
                _materials = _kandraRenderer.UseInstancedMaterials();
            }
        }

        void OnDestroy() {
            if (_kandraRenderer != null) {
                _kandraRenderer.UseOriginalMaterials();
                _materials = Array.Empty<Material>();
            }
        }

        void Update() {
            if (updateMode is UpdateMode.None) {
                return;
            }

            float deltaTime = updateMode switch {
                UpdateMode.DeltaTime => Time.deltaTime,
                UpdateMode.UnscaledDeltaTime => Time.unscaledDeltaTime,
                UpdateMode.DeltaTimeDifference => Time.unscaledDeltaTime - Time.deltaTime,
                _ => 0.0f
            };
            deltaTime *= manualPlayRate;
            _currentTime += deltaTime;

            ManuallySimulateAllVFX(deltaTime);
            ManuallySimulateComponents(deltaTime);
            ManuallySimulateMaterials();
        }

        void ManuallySimulateAllVFX(float deltaTime) {
            foreach (var vfx in _visualEffects) {
                if (!vfx.culled) {
                    vfx.Simulate(deltaTime);
                }
            }
        }
        
        void ManuallySimulateComponents(float deltaTime) {
            foreach (var component in _simulatedComponents) {
                component.Simulate(deltaTime);
            }
        }

        void ManuallySimulateMaterials() {
            if (_kandraRenderer == null) {
                return;
            }

            foreach (var material in _materials) {
                if (material.HasFloat(UnscaledTimeId)) {
                    material.SetFloat(UnscaledTimeId, _currentTime);
                }
            }
        }

        public static void AttachTo(GameObject gameObject, UpdateMode updateMode) {
            if (gameObject == null) {
                return;
            }
            
            var visualEffects = gameObject.GetComponentsInChildren<VisualEffect>();
            if (visualEffects.Length == 0) {
                return;
            }
            
            var simulator = gameObject.AddComponent<VFXManualSimulator>();
            simulator.updateMode = updateMode;
        }
        
        public enum UpdateMode : byte {
            None,
            DeltaTime,
            UnscaledDeltaTime,
            DeltaTimeDifference,
        }
    }
}
