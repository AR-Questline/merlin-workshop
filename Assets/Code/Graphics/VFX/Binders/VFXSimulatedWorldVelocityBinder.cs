using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Awaken.TG.Graphics.VFX.Binders {
    
    [AddComponentMenu("VFX/Property Binders/Simulated World Velocity Binder")]
    [VFXBinder("AR/Simulated World Velocity")] 
    public class VFXSimulatedWorldVelocityBinder : VFXBinderBase, IVFXManuallySimulated {
        public string Property { 
            [UnityEngine.Scripting.Preserve] get => (string)property;
            set => property = value;
        }

        [VFXPropertyBinding("UnityEngine.Vector3"), SerializeField]
        ExposedProperty property = "Velocity";
        
        [SerializeField] Transform target = null;

        float _manuallySimulatedTime = 0.0f;
        float _previousTime = 0.0f;
        Vector3 _previousPosition = Vector3.zero;

        public override bool IsValid(VisualEffect component) {
            return target != null && component.HasVector3(property);
        }

        public override void Reset() {
            _manuallySimulatedTime = 0.0f;
            _previousTime = 0.0f;
        }

        public override void UpdateBinding(VisualEffect component) {
            Vector3 velocity = Vector3.zero;
            float time = GetCurrentTime();

            var currentPosition = target.position;

            var deltaPos = currentPosition - _previousPosition;
            var deltaTime = time - _previousTime;
            if (deltaTime > math.EPSILON && Vector3.SqrMagnitude(deltaPos) > math.EPSILON) {
                velocity = deltaPos / deltaTime;
            }

            component.SetVector3(property, velocity);
            _previousPosition = currentPosition;
            _previousTime = time;
        }

        float GetCurrentTime() {
#if UNITY_EDITOR
            if (Application.isEditor && !Application.isPlaying) {
                return (float)UnityEditor.EditorApplication.timeSinceStartup;
            }
#endif
            return Time.time + _manuallySimulatedTime;
        }
        
        public void Simulate(float deltaTime) {
            _manuallySimulatedTime += deltaTime;
        }

        public override string ToString() {
            return $"Simulated World Velocity : '{property}' -> {(target == null ? "(null)" : target.name)}";
        }
    }
}