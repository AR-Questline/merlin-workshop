using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;

namespace Awaken.TG.Main.Animations.IK {
    [BurstCompile]
    public unsafe struct ARFootIKJob : IAnimationJob {
        ARGeneralIKData* _generalData;
        TransformStreamHandle _hipTransform;
        // --- Feet
        UnsafeArray<ARFootIKData> _feetData;
        [ReadOnly] UnsafeArray<TransformStreamHandle> _footTransforms;
        [ReadOnly] UnsafeArray<TransformStreamHandle> _targetTransforms;
        // --- Hints
        [ReadOnly] UnsafeArray<TransformStreamHandle> _kneesTransforms;
        [ReadOnly] UnsafeArray<TransformStreamHandle> _hintsTransforms;
        // --- Spine
        UnsafeArray<ARSpineIKData> _spineData;
        [ReadOnly] UnsafeArray<TransformStreamHandle>.Span _spineTransforms;
        
        public void Setup(ARGeneralIKData* generalData, TransformStreamHandle hips,
            UnsafeArray<ARFootIKData> footIKData, UnsafeArray<TransformStreamHandle> feet,
            UnsafeArray<TransformStreamHandle> targets, UnsafeArray<TransformStreamHandle> knees, UnsafeArray<TransformStreamHandle> hints,
            UnsafeArray<ARSpineIKData> spineChain, UnsafeArray<TransformStreamHandle>.Span spineTransforms) {
            _generalData = generalData;
            _hipTransform = hips;
            _feetData = footIKData;
            _footTransforms = feet;
            _targetTransforms = targets;
            _kneesTransforms = knees;
            _hintsTransforms = hints;
            _spineData = spineChain;
            _spineTransforms = spineTransforms;
        }

        public void ProcessRootMotion(AnimationStream stream) { }

        public void ProcessAnimation(AnimationStream stream) {
            if (!_generalData->isActive) {
                return;
            }

            for (uint index = 0; index < _feetData.Length; index++) {
                ref var footIKData = ref _feetData[index];
                Solve(stream, ref footIKData, _footTransforms[index], _targetTransforms[index], _kneesTransforms[index], _hintsTransforms[index]);
            }
            
            // --- Slopes
            if (_feetData.Length == 4 && _hipTransform.IsValid(stream)) {
                if (_generalData->canMove) {
                    CalculateSlope();
                }

                AlignHipsToSlope(stream);
            }
            // --- Spine Rotation
            RotateSpineToMovement(stream);
        }

        void Solve(AnimationStream stream, ref ARFootIKData data, TransformStreamHandle footHandle, TransformStreamHandle targetHandle, TransformStreamHandle kneeHandle, TransformStreamHandle hintHandle) {
            data.footAnimationPosition = footHandle.GetPosition(stream);
            data.footAnimationRotation = math.mul(_generalData->rootLocalRotation,footHandle.GetRotation(stream));
            
            quaternion rotationFromHitNormal = mathExt.FromToRotation(math.up(), data.desiredFootNormal);
            float3 footDesiredPosition = data.footAnimationPosition + data.footDesiredOffset;
            targetHandle.SetGlobalTR(stream, footDesiredPosition, math.mul(rotationFromHitNormal, data.footAnimationRotation), false);

            float positionDifference = data.footAnimationPosition.y - _generalData->currentCharacterYPosition;
            float remapped = math.remap(data.minIKHeightDifference, data.maxIKHeightDifference, 1, 0, positionDifference);
            data.desiredWeight = math.clamp(remapped, 0, 1);

            if (kneeHandle.IsValid(stream) && hintHandle.IsValid(stream)) {
                Vector3 kneeForward = _generalData->rootLocalRotation * kneeHandle.GetRotation(stream) * Vector3.forward;
                Vector3 kneePosition = mathExt.RotatePointAroundPivot(kneeHandle.GetPosition(stream),
                    _generalData->rootPosition, _generalData->rootLocalRotation);
                hintHandle.SetPosition(stream, kneePosition + kneeForward);
            }
        }

        void CalculateSlope() {
            float3 v0 = _feetData[0].raycastHitPosition;
            float3 v1 = _feetData[1].raycastHitPosition;
            float3 v2 = _feetData[2].raycastHitPosition;
            float3 v3 = _feetData[3].raycastHitPosition;

            float3 cross1 = math.normalize(math.cross(v1 - v0, v2 - v0));
            if (cross1.y < 0) {
                cross1 *= -1;
            }
            float3 cross2 = math.normalize(math.cross(v1 - v3, v2 - v3));
            if (cross2.y < 0) {
                cross2 *= -1;
            }
            float3 slope = mathExt.MoveTowards(_generalData->slopeAvgNormal, math.normalize(cross1 + cross2), VCFeetIK.LerpSpeed * _generalData->deltaTime);
            _generalData->slopeAvgNormal = math.normalizesafe(mathExt.ProjectOnPlaneUnsafe(slope, _generalData->right));
        }

        void AlignHipsToSlope(AnimationStream stream) {
            Vector3 slopeAvgNormal = _generalData->slopeAvgNormal;
            quaternion rotationFromHitNormal = mathExt.FromToRotation(math.up(), slopeAvgNormal);
            _hipTransform.SetRotation(stream, rotationFromHitNormal * _hipTransform.GetRotation(stream));

            var slopeAngle = math.acos(slopeAvgNormal.y);
            var tan = math.tan(slopeAngle);
            var yOffset = tan * _generalData->hipsToRootOffset;
            float sign = math.sign(math.dot(_generalData->forward, slopeAvgNormal));
            _hipTransform.SetPosition(stream, _hipTransform.GetPosition(stream) + Vector3.up * yOffset * sign);
        }

        void RotateSpineToMovement(AnimationStream stream) {
            if (_spineData.Length <= 0) {
                return;
            }
            
            float3 cross = math.cross(_generalData->previousForward, _generalData->forward);
            float deltaWeight = math.abs(cross.y) <= 0.01f ? 1 : math.max(0.1f, math.abs(cross.y));
            float spineRotationSpeed = _generalData->spineRotationSpeed * _generalData->deltaTime;
            quaternion desiredSpineRotation;

            if (math.abs(cross.y) <= 0.01f) {
                desiredSpineRotation = quaternion.identity;
                for (uint i = 0; i < _spineData.Length; i++) {
                    ref var spineIKData = ref _spineData[i];
                    UpdateSpineRotation(ref spineIKData, desiredSpineRotation, _spineTransforms[i]);
                }
            } else {
                for (uint i = 0; i < _spineData.Length; i++) {
                    ref var spineIKData = ref _spineData[i];
                    desiredSpineRotation = quaternion.Euler(0, spineIKData.weight * _generalData->rotationSpeed * _generalData->spineRotationStrength * cross.y, 0);
                    UpdateSpineRotation(ref spineIKData, desiredSpineRotation, _spineTransforms[i]);
                }
            }

            return;

            void UpdateSpineRotation(ref ARSpineIKData spineIKData, quaternion spineRotation, TransformStreamHandle spineTransform) {
                spineIKData.spineAnimationRotation = mathExt.RotateTowardsRadians(spineIKData.spineAnimationRotation, spineRotation, spineRotationSpeed * deltaWeight);
                Quaternion spineElementRotation = spineTransform.GetRotation(stream);
                spineTransform.SetRotation(stream, spineIKData.spineAnimationRotation * spineElementRotation);
            }
        }
    }
}