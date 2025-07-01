using System.Collections.Generic;
using Awaken.TG.Utility;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Utility.PhysicsUtils {
    /// <summary>
    /// Handler for CharacterController movement, which dynamically adjusts slope limit
    /// to prevent character from getting stuck in unusual geometry, and provides more 
    /// detailed information about hits.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CharacterControllerTweakedMovementHandler : MonoBehaviour {
        const float AlmostVerticalAngle = 89f;
        
        [SerializeField] int framesToAccumulateHitsFor = 5;
        [SerializeField] LayerMask groundLayers;

        Transform _transform;
        CharacterController _controller;
        
        Vector3 _groundNormal;
        Vector3 _hitNormal;
        float _baseSlopeLimit;
        float _baseSlopeLimitCos;
        float _baseSlopeLimitTan;

        readonly Queue<Vector3> _accumulatedHitNormals = new();
        readonly Queue<int> _accumulatedHitsCountPerFrame = new();
        int _accumulatedHitsCountThisFrame;
        
        public bool Grounded => _groundNormal != Vector3.zero;
        public Vector3 GroundNormal => _groundNormal;
        public Vector3 HitNormal => _hitNormal;
        
        void Awake() {
            _transform = transform;
            _controller = GetComponent<CharacterController>();
            SetBaseSlopeLimit(_controller.slopeLimit);
        }
        
        void OnControllerColliderHit(ControllerColliderHit hit) {
            AccumulateHit(hit);
        }
        
        void AccumulateHit(ControllerColliderHit hit) {
            _accumulatedHitNormals.Enqueue(hit.normal);
            _accumulatedHitsCountThisFrame++;
        }

        public void OnHeroJumped() {
            _groundNormal = Vector3.zero;
        }
        
        public void SetGroundMask(LayerMask mask) {
            groundLayers = mask;
        }

        public void SetBaseSlopeLimit(float slopeLimit) {
            _baseSlopeLimit = slopeLimit;
            _controller.slopeLimit = slopeLimit;
            _baseSlopeLimitCos = math.cos(slopeLimit * math.TORADIANS);
            _baseSlopeLimitTan = math.tan(slopeLimit * math.TORADIANS);
        }
        
        public void PerformMoveStep(Vector3 moveDelta) {
            if (Grounded && HasValidSpaceToPerformStep(moveDelta)) {
                _controller.slopeLimit = AlmostVerticalAngle;
            }
            
            _controller.Move(moveDelta);
            
            _controller.slopeLimit = _baseSlopeLimit;
            
            ResolveAccumulatedHits();
        }

        void ResolveAccumulatedHits() {
            if (_accumulatedHitsCountThisFrame == 0) {
                ClearAccumulatedHitsQueue();
                _hitNormal = Vector3.zero;
                _groundNormal = Vector3.zero;
            } else {
                DetermineHitStatesFromAccumulatedHits();
                MoveAccumulatedHitsQueue();
            }
        }
        
        void ClearAccumulatedHitsQueue() {
            _accumulatedHitsCountPerFrame.Clear();
            _accumulatedHitNormals.Clear();
        }

        void DetermineHitStatesFromAccumulatedHits() {
            _hitNormal = GetMidRangeHitNormal(grounded: false);
            _groundNormal = GetMidRangeHitNormal(grounded: true);
            
            if (_groundNormal == Vector3.zero && CanStandOnSurface(_hitNormal)) {
                _groundNormal = _hitNormal;
            }
        }
        
        Vector3 GetMidRangeHitNormal(bool grounded) {
            Bounds bounds = new(Vector3.zero, Vector3.zero);
            
            foreach (Vector3 hit in _accumulatedHitNormals) {
                if (grounded && !CanStandOnSurface(hit)) {
                    continue;
                }
                bounds.Encapsulate(hit);
            }
            
            return bounds.center.normalized;
        }
        
        bool CanStandOnSurface(Vector3 normal) {
            return normal.y >= _baseSlopeLimitCos;
        }
        
        void MoveAccumulatedHitsQueue() {
            _accumulatedHitsCountPerFrame.Enqueue(_accumulatedHitsCountThisFrame);
            _accumulatedHitsCountThisFrame = 0;

            if (_accumulatedHitsCountPerFrame.Count > framesToAccumulateHitsFor) {
                int hitsToDequeue = _accumulatedHitsCountPerFrame.Dequeue();

                for (int i = 0; i < hitsToDequeue; i++) {
                    _accumulatedHitNormals.Dequeue();
                }
            }
        }
        
        bool HasValidSpaceToPerformStep(Vector3 moveDelta) {
            if (moveDelta == Vector3.zero) return true;

            Vector3 slopeCheckDirection = (_hitNormal * -1).ToHorizontal3();
            if (slopeCheckDirection.magnitude < 0.01f) slopeCheckDirection = moveDelta.ToHorizontal3();

            float maxStep = GetLargestPossibleStepHeight();
            float maxStepDistance = maxStep / _baseSlopeLimitTan;
            Vector3 verticalStep = maxStep * Vector3.up;
            Vector3 horizontalStep = slopeCheckDirection.normalized * maxStepDistance;
            
            float sphereCenterOffset = math.max(0.0f, _controller.height * 0.5f - _controller.radius);
            Vector3 capsuleCenter = _transform.position + _controller.center + horizontalStep + verticalStep;
            
            Vector3 capsuleStart = capsuleCenter + Vector3.up * sphereCenterOffset;
            Vector3 capsuleEnd = capsuleCenter + Vector3.down * sphereCenterOffset;
            return !Physics.CheckCapsule(capsuleStart, capsuleEnd, _controller.radius, groundLayers, QueryTriggerInteraction.Ignore);
        }

        // Returns the highest step which controller can climb. CharacterController's stepOffset defines the maximum
        // collision height of triggering the step logic, but the step itself can also be climbed through a regular
        // slope movement determined by controller's slope limit. This method calculates the sum of these two values.
        float GetLargestPossibleStepHeight() {
            float controllerStep = _controller.stepOffset;
            float feetMaxGroundOffset = (1.0f - _baseSlopeLimitCos) * _controller.radius;
            
            return controllerStep + feetMaxGroundOffset;
        }
    }
}