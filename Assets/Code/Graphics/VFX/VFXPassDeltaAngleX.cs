using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Graphics.VFX {
    [ExecuteAlways]
    public class VFXPassDeltaAngleX : MonoBehaviour, IVFXManuallySimulated {
        const string DeltaPositionPropertyName = "DeltaPosition";
        [SerializeField] float _bias = 0.001f;
        [SerializeField] float _multiplier = 1f;
        [SerializeField] float _timeMultiplier = 3f;
        [SerializeField] float _timeMultiplierReturn = 0.6f;
        [SerializeField] VisualEffect _visualEffect;
        Vector3 _oldPosition;
        float _currentDeltaX;
        float _timePassed;
        bool _wasLerping;
        bool _hasProperty;

        void Start() {
            if (!_visualEffect)
                _visualEffect = GetComponent<VisualEffect>();
            
            _hasProperty = _visualEffect.HasFloat(DeltaPositionPropertyName);
        }

        void Update() {
            Simulate(Time.deltaTime);
        }

        public void Simulate(float deltaTime) {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                _hasProperty = _visualEffect.HasFloat(DeltaPositionPropertyName);
#endif
            if (!_hasProperty || deltaTime == 0f) {
                return;
            }
            Vector3 heading = _oldPosition - transform.position;
            float distance = (_oldPosition - transform.position).magnitude;
            float targetDeltaX;
            float timeModifier = 1f;
            
            if (distance > _bias) {
                _timePassed = 0f;
                Vector3 direction = heading / distance;
                targetDeltaX = AngleDir(direction);
                _wasLerping = true;
            } else {
                if (_wasLerping) {
                    _wasLerping = false;
                    _timePassed = 0;
                }
                timeModifier = _timeMultiplierReturn;
                targetDeltaX = 0;
            }

            _timePassed += _timeMultiplier * Time.deltaTime * timeModifier;
            _currentDeltaX = Mathf.Lerp(_currentDeltaX, targetDeltaX, _timePassed);
            _visualEffect.SetFloat(DeltaPositionPropertyName, _currentDeltaX);

            _oldPosition = transform.position;
            if (_timePassed > 1)
                _timePassed = 0;
        }

        float AngleDir(Vector3 targetDir) {
            Vector3 perp = Vector3.Cross(transform.forward, targetDir);
            return Vector3.Dot(perp, Vector3.up) * _multiplier;
        }
    }
}