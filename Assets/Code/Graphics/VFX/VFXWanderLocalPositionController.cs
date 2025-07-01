using Awaken.TG.Code.Utility;
using Awaken.TG.Utility;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Graphics.VFX {
    public class VFXWanderLocalPositionController : MonoBehaviour {
        const int AxisIndexX = 0;
        const int AxisIndexY = 1;
        const int AxisIndexZ = 2;
        
        [SerializeField] Vector3 zoneAxisRanges;
        [SerializeField] Vector3 wanderSpeeds;
        [SerializeField] float centerAvoidanceScale;

        Vector3 _interpolationState;
        Vector3 _interpolationSpeeds;
        Vector3 _targetPositions;
        Vector3 _initialPositions;

        Transform _transform;
        Vector3 _sphereCenter;
        Vector3 _currentPosition;
        
        void OnEnable() {
            _transform = transform;
            _sphereCenter = _transform.localPosition;
            _currentPosition = _sphereCenter;
            
            SetupNewAxisTarget(AxisIndexX);
            SetupNewAxisTarget(AxisIndexY);
            SetupNewAxisTarget(AxisIndexZ);
        }

        void Update() {
            if (Time.deltaTime == 0.0f) {
                return;
            }
            
            HandleAxisMovement(AxisIndexX);
            HandleAxisMovement(AxisIndexY);
            HandleAxisMovement(AxisIndexZ);

            _transform.localPosition = _currentPosition;
        }

        void HandleAxisMovement(int axisIndex) {
            float lerpDelta = Easing.Cubic.InOut(_interpolationState[axisIndex]);
            float newPosition = math.lerp(_initialPositions[axisIndex], _targetPositions[axisIndex], lerpDelta);
            _currentPosition[axisIndex] = newPosition;
            
            _interpolationState[axisIndex] += _interpolationSpeeds[axisIndex] * Time.deltaTime;
            
            if (_interpolationState[axisIndex] >= 1) {
                SetupNewAxisTarget(axisIndex);
            }
        }

        void SetupNewAxisTarget(int axisIndex) {
            _initialPositions[axisIndex] = _currentPosition[axisIndex];
            _interpolationState[axisIndex] = 0;

            float randomLocalPosition = GetRandomLocalAxisPositionOppositeToCurrent(axisIndex);
            _targetPositions[axisIndex] = _sphereCenter[axisIndex] + randomLocalPosition;

            float distance = math.abs(_targetPositions[axisIndex] - _initialPositions[axisIndex]);
            _interpolationSpeeds[axisIndex] = wanderSpeeds[axisIndex] / distance;
        }

        float GetRandomLocalAxisPositionOppositeToCurrent(int axisIndex) {
            float range = zoneAxisRanges[axisIndex];
            float localPositionSign = math.sign(_currentPosition[axisIndex] - _sphereCenter[axisIndex]);
            
            if (localPositionSign == 0f) {
                return RandomUtil.UniformFloat(-range, range);
            }

            float absRandomPos = RandomUtil.UniformFloat(range * centerAvoidanceScale, range);
            float oppositeSideRandomPos = absRandomPos * localPositionSign * -1.0f;
            return oppositeSideRandomPos;
        }
    }
}