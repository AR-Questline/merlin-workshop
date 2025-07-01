using System.Diagnostics;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Utility;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat {
    public class CircleAroundData {
        const float MinimumDistanceToRecalculate = 3f;
        const float MinimumDistanceToRecalculateSqr = MinimumDistanceToRecalculate * MinimumDistanceToRecalculate;
        const int AmountOfPoints = 16;
        const float MaxDistance = 15f;
        const float MinDistance = 7f;

        static bool DebugMode => SafeEditorPrefs.GetBool("debug.circling");
        
        readonly ICharacter _target;
        readonly RaycastCheck _raycastCheck;
        int _version;
#if DEBUG
        GameObject[] _pointVisuals;
#endif
        CirclePoint[] _points;
        Vector3 _lastCalculationPosition;
        float _nextAttackTime;

        public CircleAroundData(ICharacter target) {
            _target = target;
            _raycastCheck = new RaycastCheck {
                accept = GameConstants.Get.obstaclesMask
            };

            CreateDebugVisuals();
            CreatePoints();
            CalculateNewPositions();
        }

        public bool CanLeaveCircling() {
            return _nextAttackTime < Time.time;
        }
        
        public void LeaveCircling(float cooldown) {
            _nextAttackTime = Time.time + cooldown;
        }
        
        public Vector3 GetClosestPoint(in Vector3 position, in Vector3 forward, out int lastVersion, out int bestIndex, in float offset) {
            if (offset > 0) {
                return GetOffsettedPoint(GetClosestPointInternal(in position, in forward, out lastVersion, out bestIndex), offset);
            }
            return GetClosestPointInternal(in position, in forward, out lastVersion, out bestIndex).position;
        }

        CirclePoint GetClosestPointInternal(in Vector3 position, in Vector3 forward, out int lastVersion, out int bestIndex) {
            TryUpdate();
            lastVersion = _version;

            bestIndex = 0;
            float bestValue = float.MaxValue;
            for(int i = 0; i < AmountOfPoints; i++) {
                if (_points[i].isValid) {
                    Vector3 direction = _points[i].position - position;
                    float dot = Vector3.Dot(forward, direction);
                    // 1.0 - 0.5 => 1
                    // 0.5 - 0.0 => 1->6
                    // 0.0 - -0.5 => 6->10
                    // -0.5 - -1.0 => 10->15
                    dot = dot > 0.5 ? 1f : 
                        dot > 0 ? dot.Remap(0f, 0.5f, 32f, 1f) : 
                        dot > -0.5f ? dot.Remap(-0.5f, 0, 100f, 32f) :
                        dot.Remap(-1f, -0.5f, 225f, 100f);
                    float value = direction.sqrMagnitude * dot;
                    if (value < bestValue) {
                        bestValue = value;
                        bestIndex = i;
                    }
                }
            }
            
            return _points[bestIndex];
        }

        public Vector3 GetNextPoint(in Vector3 position, in Vector3 forward, ref int lastVersion, ref int currentIndex, in bool ascendingDirection, in float offset, out bool changedDirection) {
            if (offset > 0) {
                return GetOffsettedPoint(GetNextPointInternal(in position, in forward, ref lastVersion, ref currentIndex, in ascendingDirection, out changedDirection), offset);
            }
            return GetNextPointInternal(in position, in forward, ref lastVersion, ref currentIndex, in ascendingDirection, out changedDirection).position;
        }
        
        CirclePoint GetNextPointInternal(in Vector3 position, in Vector3 forward, ref int lastVersion, ref int currentIndex, in bool ascendingDirection, out bool changedDirection) {
            changedDirection = false;
            if (TryUpdate() || lastVersion != _version) {
                return GetClosestPointInternal(in position, in forward, out lastVersion, out currentIndex);
            }
            
            int nextIndex;
            if (ascendingDirection) {
                nextIndex = (currentIndex + 1) % AmountOfPoints;
                if (_points[nextIndex].isValid) {
                    currentIndex = nextIndex;
                    return _points[nextIndex];
                }

                // Reverse direction
                changedDirection = true;
                nextIndex = (currentIndex + AmountOfPoints - 1) % AmountOfPoints;
                if (_points[nextIndex].isValid) {
                    currentIndex = nextIndex;
                    return _points[nextIndex];
                }
            } else {
                nextIndex = (currentIndex + AmountOfPoints - 1) % AmountOfPoints;
                if (_points[nextIndex].isValid) {
                    currentIndex = nextIndex;
                    return _points[nextIndex];
                }

                // Reverse direction
                changedDirection = true;
                nextIndex = (currentIndex + 1) % AmountOfPoints;
                if (_points[nextIndex].isValid) {
                    currentIndex = nextIndex;
                    return _points[nextIndex];
                }
            }

            return _points[currentIndex];
        }

        Vector3 GetOffsettedPoint(in CirclePoint point, float targetOffset) {
            return point.position - point.directionToSearch * math.min(point.distance - MinDistance, targetOffset);
        }
        
        void CreatePoints() {
            _points = new CirclePoint[AmountOfPoints];
            for(int i = 0; i < AmountOfPoints; i++) {
                var direction = new Vector3(Mathf.Cos(i * 2 * Mathf.PI / AmountOfPoints), 0, Mathf.Sin(i * 2 * Mathf.PI / AmountOfPoints));
                _points[i] = new CirclePoint(direction);
            }
        }

        bool TryUpdate() {
            if ((_lastCalculationPosition - _target.Coords).sqrMagnitude > MinimumDistanceToRecalculateSqr) {
                CalculateNewPositions();
                return true;
            }
            return false;
        }
        
        void CalculateNewPositions() {
            _version++;
            _lastCalculationPosition = _target.Coords;
            for(int i = 0; i < AmountOfPoints; i++) {
                TrySetFurthestPoint(ref _points[i]);
            }
            MoveDebugVisuals();
        }

        void TrySetFurthestPoint(ref CirclePoint circlePoint) {
            const float SafetyDistanceDecrease = 0.2f;
            const float DistanceFromHit = 0.6f;
            
            float distance = MaxDistance;
            while (distance >= MinDistance) {
                var furthestPoint = Ground.SnapToGround(_lastCalculationPosition + circlePoint.directionToSearch * distance);
                var newDirection = furthestPoint - _lastCalculationPosition;
                if (IsVisible(_lastCalculationPosition + Vector3.up, newDirection, newDirection.magnitude - SafetyDistanceDecrease, out var hitPoint)) {
                    if (HasEnoughPlaceAround(furthestPoint)) {
                        circlePoint.position = furthestPoint;
                        circlePoint.isValid = true;
                        circlePoint.distance = distance;
                        return;
                    } else {
                        distance -= DistanceFromHit;
                        continue;
                    }
                }
                distance = (_lastCalculationPosition.ToHorizontal2() - hitPoint.ToHorizontal2()).magnitude - DistanceFromHit;
            }
            circlePoint.isValid = false;
        }

        bool IsVisible(Vector3 from, Vector3 direction, float distance, out Vector3 hitPoint) {
            var result = _raycastCheck.Raycast(from, direction, distance, 0);
            hitPoint = result.Point;
            return result.Collider == null;
        }

        bool HasEnoughPlaceAround(Vector3 point) {
            const float Height = 2f;
            const float Radius = 0.5f;
            foreach (var _ in _raycastCheck.OverlapTargetsCapsule(point + Vector3.up, Vector3.up, Height, Radius, 0)) {
                return false;
            }
            return true;
        }

        [Conditional("DEBUG")]
        void CreateDebugVisuals() {
#if DEBUG
            _pointVisuals = new GameObject[AmountOfPoints];
            for(int i = 0; i < AmountOfPoints; i++) {
                _pointVisuals[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(_pointVisuals[i].GetComponent<Collider>());
            }
#endif
        }

        [Conditional("DEBUG")]
        public void ClearDebugVisuals() {
#if DEBUG
            foreach (var point in _pointVisuals) {
                GameObject.Destroy(point);
            }
#endif
        }

        [Conditional("DEBUG")]
        void MoveDebugVisuals() {
#if DEBUG
            for(int i = 0; i < AmountOfPoints; i++) {
                if (!_points[i].isValid) {
                    _pointVisuals[i].SetActive(false);
                    continue;
                }
                _pointVisuals[i].SetActive(DebugMode);
                _pointVisuals[i].transform.position = _points[i].position;
            }
#endif
        }
    }
    
    struct CirclePoint {
        public readonly Vector3 directionToSearch;
        public Vector3 position;
        public float distance;
        public bool isValid;

        public CirclePoint(Vector3 directionToSearch) {
            this.directionToSearch = directionToSearch;
            position = default;
            distance = 0;
            isValid = false;
        }
    }
}
