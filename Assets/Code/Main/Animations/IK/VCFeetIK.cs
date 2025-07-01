using System;
using Animancer;
using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Pathfinding;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using Vector3 = UnityEngine.Vector3;

namespace Awaken.TG.Main.Animations.IK {
    [RequireComponent(typeof(ARNpcAnimancer))]
    public unsafe class VCFeetIK : ViewComponent<Location> {
        public const float LerpSpeed = 2.5f;
        public const float FootPositionLerpSpeed = 2.5f;
        public const float FootRotationLerpSpeed = 2.5f;

        ARGeneralIKData* _generalData; // shared data
        UnsafeArray<ARFootIKData> _footData;
        UnsafeArray<TransformStreamHandle> _transformsData;
        UnsafeArray<TransformStreamHandle>.Span _footTransforms;
        UnsafeArray<TransformStreamHandle>.Span _targetTransforms;
        UnsafeArray<TransformStreamHandle>.Span _kneesTransforms;
        UnsafeArray<TransformStreamHandle>.Span _hintsTransforms;
        UnsafeArray<ARSpineIKData> _spineData;
        UnsafeArray<TransformStreamHandle> _spineTransforms;
        AnimationScriptPlayable _jobPlayable;

        [SerializeField, Title("General Setup"), Range(0, 5)] int enableInDistanceBand = 1;
        [SerializeField] float addedHeight = 3f;
        [SerializeField] float maxHitDistance = 5f;
        [SerializeField] Transform rootTransform;
        [SerializeField, Title("Feet IK")] IKFootSetup[] feet = Array.Empty<IKFootSetup>();
        [SerializeField, Title("SpineRotation")] IKSpineSetup[] spineChain = Array.Empty<IKSpineSetup>();
        [SerializeField] Transform hipsTransform;
        [SerializeField] float spineRotationStrength = 0.2f;
        [SerializeField] float spineRotationSpeed = 30f;

        ARNpcAnimancer _npcAnimancer;
        RichAI _richAI;
        WeakModelRef<NpcElement> _npcElement;
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly] bool _created;
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly] bool _initialized;

        protected override void OnAttach() {
            if (Target.Character == null) {
                return;
            }

            _generalData = (ARGeneralIKData*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<ARGeneralIKData>(), UnsafeUtility.AlignOf<ARGeneralIKData>(), Allocator.Persistent);
            *_generalData = new ARGeneralIKData {
                isActive = true,
                currentCharacterYPosition = Target.Coords.y,
                hipsToRootOffset = hipsTransform != null
                    ? (hipsTransform.position.ToHorizontal3() - hipsTransform.parent.position.ToHorizontal3()).magnitude
                    : 0,
                forward = Target.Forward(),
                previousForward = Target.Forward(),
                slopeAvgNormal = math.up(),
                rotationSpeed = 0,
                deltaTime = Target.GetDeltaTime(),
                spineRotationStrength = spineRotationStrength,
                spineRotationSpeed = spineRotationSpeed * mathExt.DegreeToRadian
            };
            
            var uCount = (uint)feet.Length;
            _footData = new UnsafeArray<ARFootIKData>(uCount, Allocator.Persistent);
            _transformsData = new UnsafeArray<TransformStreamHandle>(uCount * 4, Allocator.Persistent);
            var bufferPointer = _transformsData.Ptr;
            _footTransforms = UnsafeArray<TransformStreamHandle>.FromExistingData(bufferPointer, uCount);
            bufferPointer += uCount;
            _targetTransforms = UnsafeArray<TransformStreamHandle>.FromExistingData(bufferPointer, uCount);
            bufferPointer += uCount;
            _kneesTransforms = UnsafeArray<TransformStreamHandle>.FromExistingData(bufferPointer, uCount);
            bufferPointer += uCount;
            _hintsTransforms = UnsafeArray<TransformStreamHandle>.FromExistingData(bufferPointer, uCount);

            Animator animator = GetComponent<Animator>();
            _npcAnimancer = GetComponent<ARNpcAnimancer>();
            for (uint i = 0; i < feet.Length; i++) {
                IKFootSetup ikFoot = feet[i];
                Transform footTransform = ikFoot.footTransform;
                if (footTransform == null) {
                    Log.Critical?.Error("VCFeetIK: IKFootSetup.footTransform is null", gameObject);
                }
                var dataElement = new ARFootIKData {
                    footAnimationPosition = footTransform.position,
                    footAnimationRotation = footTransform.rotation,
                    desiredWeight = 1,
                    minIKHeightDifference = ikFoot.disableAtHeightDifference / 2f,
                    maxIKHeightDifference = ikFoot.disableAtHeightDifference,
                    desiredFootNormal = new float3(0, 1, 0)
                };
                _footData[i] = dataElement;
                _footTransforms[i] = animator.BindStreamTransform(ikFoot.footTransform);
                _targetTransforms[i] = animator.BindStreamTransform(ikFoot.targetTransform);

                if (ikFoot.kneeTransform != null && ikFoot.hintTransform != null) {
                    _kneesTransforms[i] = animator.BindStreamTransform(ikFoot.kneeTransform);
                    _hintsTransforms[i] = animator.BindStreamTransform(ikFoot.hintTransform);
                }
            }

            _spineData = new UnsafeArray<ARSpineIKData>((uint)spineChain.Length, ARAlloc.Persistent);
            _spineTransforms = new UnsafeArray<TransformStreamHandle>((uint)spineChain.Length, ARAlloc.Persistent);
            for (uint i = 0; i < spineChain.Length; i++) {
                IKSpineSetup ikSpine = spineChain[i];
                if (ikSpine.spineElement == null) {
                    Log.Critical?.Error("VCFeetIK: IKSpineSetup.spineElement is null", gameObject);
                }
                var spineData = new ARSpineIKData {
                    spineAnimationRotation = Quaternion.identity,
                    weight = ikSpine.weight
                };
                _spineData[i] = spineData;
                _spineTransforms[i] = animator.BindStreamTransform(ikSpine.spineElement);
            }
            
            // --- Setup Job
            ARFootIKJob job = new();
            TransformStreamHandle hipsStreamTransform = hipsTransform != null ? animator.BindStreamTransform(hipsTransform) : default;
            job.Setup(_generalData, hipsStreamTransform, _footData, _footTransforms.AsUnsafeArray(),
                _targetTransforms.AsUnsafeArray(), _kneesTransforms.AsUnsafeArray(), _hintsTransforms.AsUnsafeArray(),
                _spineData, _spineTransforms);
            _jobPlayable = _npcAnimancer.Playable.InsertOutputJob(job);
            
            Target.Character.ListenTo(IAlive.Events.BeforeDeath, Dispose, this);

            // --- Assign Ground Layers
            if (Target.TryGetElement(out NpcElement npcElement)) {
                _npcElement = npcElement;
                npcElement.ListenTo(NpcInteractor.Events.InteractionChanged, OnInteractionChanged, this);
                npcElement.OnCompletelyInitialized(_ => {
                    if (!_created) {
                        return;
                    }
                    
                    _richAI = npcElement.Controller.RichAI;
                    _initialized = true;
                    
                    UpdateIsActive(npcElement.ParentModel.GetCurrentBandSafe(999), npcElement);
                    
                    if (_generalData->isActive) {
                        Target.GetOrCreateTimeDependent().WithLateUpdate(OnLateUpdate);
                    }
                });
            }
            
            // --- Listeners
            Target.ListenTo(ICullingSystemRegistreeModel.Events.DistanceBandChanged, OnDistanceBandChanged, this);
            _created = true;
        }

        void OnInteractionChanged() {
            if (!_npcElement.TryGet(out NpcElement npcElement)) {
                return;
            }
            UpdateIsActive(npcElement.ParentModel.GetCurrentBandSafe(999), npcElement);
        }
        
        void OnDistanceBandChanged(int currentBand) {
            UpdateIsActive(currentBand, _npcElement.Get());
        }

        void UpdateIsActive(int currentBand, NpcElement npcElement) {
            bool wasActive = _generalData->isActive;
            bool currentInteractionCondition = npcElement?.Interactor.CurrentInteraction?.AllowUseIK ?? true;
            bool shouldBeActive = currentBand <= enableInDistanceBand && currentInteractionCondition;
            
            if (wasActive != shouldBeActive) {
                _generalData->isActive = shouldBeActive;
                
                if (!shouldBeActive) {
                    feet.ForEach(f => f.constraint.weight = 0);
                }

                if (_initialized) {
                    if (shouldBeActive) {
                        Target.GetOrCreateTimeDependent().WithLateUpdate(OnLateUpdate);
                    } else {
                        Target.GetOrCreateTimeDependent().WithoutLateUpdate(OnLateUpdate);
                    }
                }
            }
        }
        
        void OnLateUpdate(float deltaTime) {
            // --- General Data update
            _generalData->canMove = _richAI.canMove;
            _generalData->previousForward = _generalData->forward;
            _generalData->forward = Target.Forward();
            if (rootTransform != null) {
                _generalData->rootPosition = rootTransform.position;
                _generalData->rootLocalRotation = rootTransform.localRotation;
            } else {
                _generalData->rootPosition = Target.Coords;
                _generalData->rootLocalRotation = Quaternion.identity;
            }
            _generalData->right = Target.Right();
            _generalData->currentCharacterYPosition = Target.Coords.y;
            _generalData->deltaTime = deltaTime;
            _generalData->rotationSpeed = _richAI.rotationSpeed;
            
            // --- Feet IK
            for (uint index = 0; index < feet.Length; index++) {
                var dataElement = _footData[index];
                float3 pos = mathExt.RotatePointAroundPivot(dataElement.footAnimationPosition,
                    _generalData->rootPosition, _generalData->rootLocalRotation);

                IKFootSetup ikFootSetup = feet[index];
                float movementWeight = _npcAnimancer.MovementSpeed > 0.25f ? 0.75f : 1;
                float ikWeight = movementWeight * dataElement.desiredWeight;
                ikFootSetup.constraint.weight = Mathf.MoveTowards(ikFootSetup.constraint.weight, ikWeight, LerpSpeed * deltaTime);
                
                bool groundHit = CheckGroundBelowFeet(pos, out Vector3 hitNormal, out Vector3 hitPoint);
                Vector3 desiredHitNormal;
                if (groundHit) {
                    desiredHitNormal = hitNormal;
                    hitPoint.y += ikFootSetup.yOffset;
                    dataElement.raycastHitPosition = hitPoint;
                } else {
                    desiredHitNormal = math.up();
                    dataElement.raycastHitPosition = pos;
                }

                dataElement.desiredFootNormal = mathExt.MoveTowards(dataElement.desiredFootNormal, desiredHitNormal, FootRotationLerpSpeed * deltaTime);
                
                float3 desiredOffset = dataElement.raycastHitPosition - dataElement.footAnimationPosition;
                dataElement.footDesiredOffset = mathExt.MoveTowards(dataElement.footDesiredOffset, desiredOffset, FootPositionLerpSpeed * deltaTime);
                _footData[index] = dataElement;
            }
        }

        bool CheckGroundBelowFeet(Vector3 footPosition, out Vector3 hitNormal, out Vector3 hitPoint) {
            Vector3 startSphereCast = footPosition + Vector3.up * addedHeight;
            if (Physics.SphereCast(startSphereCast, 0.2f, Vector3.down, out RaycastHit hit, maxHitDistance, RenderLayers.Mask.CharacterGround, QueryTriggerInteraction.Ignore)) {
                hitNormal = hit.normal;
                hitPoint = hit.point;
                return true;
            }
            
            hitNormal = Vector3.up;
            hitPoint = footPosition;
            return false;
        }
        
        void Dispose() {
            if (!_created) {
                return;
            }

            World.EventSystem.RemoveAllListenersOwnedBy(this);
            AnimancerUtilities.RemovePlayable(_jobPlayable);
            _footData.Dispose();
            _transformsData.Dispose();
            _spineData.Dispose();
            _spineTransforms.Dispose();
            UnsafeUtility.Free(_generalData, Allocator.Persistent);
            Target.GetTimeDependent()?.WithoutLateUpdate(OnLateUpdate);
            _created = false;
        }
        
        protected override void OnDiscard() {
            Dispose();
        }

        [Serializable]
        struct IKFootSetup {
            [Title("Foot Setup")] 
            public Transform footTransform;
            public Transform targetTransform;
            public TwoBoneIKConstraint constraint;
            public float yOffset;
            public float disableAtHeightDifference;
            [Title("Knees & Hints Setup")]
            public Transform kneeTransform;
            public Transform hintTransform;
        }

        [Serializable]
        struct IKSpineSetup {
            public Transform spineElement;
            public float weight;
        }
    }
}
