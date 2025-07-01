using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Graphics.VFX {
    [ExecuteAlways]
    public class VFXPassDeltaPosition : MonoBehaviour, IVFXManuallySimulated {
        const string DeltaPositionPropertyName = "DeltaPosition";
        [SerializeField] float _bias = 0.001f;
        [SerializeField] float _timeMultiplier = 3f;
        [SerializeField] float _timeMultiplierReturn = 0.6f;
        [SerializeField] VisualEffect _visualEffect;
        Vector3 _oldPosition;
        Vector3 _currentDeltaPosition;
        float _timePassed;
        bool _wasLerping;
        bool _hasProperty;
        Transform _transform;
        
        void Awake() {
            if (!_visualEffect) {
                _visualEffect = GetComponent<VisualEffect>();
            }

            _hasProperty = _visualEffect.HasVector3(DeltaPositionPropertyName);
            _transform = transform;
        }

        void OnEnable() {
            _currentDeltaPosition = _transform.position;
            _oldPosition = _currentDeltaPosition;
        }

        void Update() {
            Simulate(Time.deltaTime);
        }

        public void Simulate(float deltaTime) {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                _hasProperty = _visualEffect.HasVector3(DeltaPositionPropertyName);
#endif
            if (!_hasProperty || deltaTime == 0f) {
                return;
            }
            
            float distance = (_oldPosition - _transform.position).magnitude;
            Vector3 targetDeltaPos;
            float timeModifier = 1f;
            
            if (distance > _bias) {
                _timePassed = 0f;
                targetDeltaPos = _oldPosition;
                _wasLerping = true;
            } else {
                if (_wasLerping) {
                    _wasLerping = false;
                    _timePassed = 0;
                }
                timeModifier = _timeMultiplierReturn;
                targetDeltaPos = _transform.position;
            }

            _timePassed += _timeMultiplier * deltaTime * timeModifier;
            _currentDeltaPosition = Vector3.Slerp(_currentDeltaPosition, targetDeltaPos, _timePassed);
            _visualEffect.SetVector3(DeltaPositionPropertyName, _currentDeltaPosition);

            _oldPosition = _transform.position;
            if (_timePassed > 1)
                _timePassed = 0;
        }
    }
}