using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using System;
using Animancer;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Serialization;

namespace Awaken.TG.Main.Animations.IK {
    [RequireComponent(typeof(ARHeroAnimancer))]
    public unsafe class VCTppSpineIK : ViewComponent<Hero> {
        const float BottomClampUpdateSpeed = 50f;
        const float WeightLerpSpeed = 2f;
        const float FullyActiveIkWeight = 1f;
        const float OutOfFightIkWeight = 0.33f;
        const float DisabledIkWeight = 0f;
        [SerializeField] SpineRotationSetup[] transformsToRotate = Array.Empty<SpineRotationSetup>();
        
        ARHeroSpineGeneralIKData* _generalData; // shared data
        UnsafeArray<ARHeroSpineIKData> _spineData;
        UnsafeArray<TransformStreamHandle> _transformsData;
        AnimationScriptPlayable _jobPlayable;

        DelayedValue _bottomClamp;
        bool _isProcessingAnimationSpeed;
        bool _created;

        protected override void OnAttach() {
            Animator animator = GetComponent<Animator>();
            ARHeroAnimancer heroAnimancer = GetComponent<ARHeroAnimancer>();
            
            _generalData = (ARHeroSpineGeneralIKData*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<ARHeroSpineGeneralIKData>(), UnsafeUtility.AlignOf<ARHeroSpineGeneralIKData>(), Allocator.Persistent);
            _generalData->ikStrength = 1;
            
            var uCount = (uint)transformsToRotate.Length;
            _spineData = new UnsafeArray<ARHeroSpineIKData>(uCount, Allocator.Persistent);
            _transformsData = new UnsafeArray<TransformStreamHandle>(uCount, Allocator.Persistent);
            for (uint i = 0; i < transformsToRotate.Length; i++) {
                SpineRotationSetup spineRotationSetup = transformsToRotate[i];
                
                Transform spineTransform = spineRotationSetup.transformToRotate;
                _transformsData[i] = animator.BindStreamTransform(spineTransform);
                
                _spineData[i] = new ARHeroSpineIKData {
                    weightX = spineRotationSetup.weightX,
                    weightY = spineRotationSetup.weightY,
                    weightZ = spineRotationSetup.weightZ,
                    constraint = spineRotationSetup.constraint,
                    baseWeight = 1,
                };
            }
            
            // --- Setup Job
            ARHeroSpineIKJob job = new();
            job.Setup(_generalData, _spineData, _transformsData);
            _jobPlayable = heroAnimancer.Playable.InsertOutputJob(job);
            
            Target.GetOrCreateTimeDependent().WithLateUpdate(OnLateUpdate);
            
            _bottomClamp.SetInstant(Target.Data.tppBottomClamp);

            Target.ListenTo(Hero.Events.ProcessAnimationSpeed, () => _isProcessingAnimationSpeed = true, this);
            Target.ListenTo(Hero.Events.StopProcessingAnimationSpeed, () => _isProcessingAnimationSpeed = false, this);
            
            _created = true;
        }

        void OnLateUpdate(float deltaTime) {
            if (!_created) {
                return;
            }

            bool isActive = !Target.Mounted;
            _generalData->isActive = isActive;
            if (!isActive) {
                return;
            }
            
            bool isDrawingBow = Target.TryGetElement<BowFSM>()?.CurrentAnimatorState?.GeneralType == HeroGeneralStateType.BowDraw;
            bool isUsingTwoHanded = Target.MainHandItem is { IsTwoHanded: true, IsFists: false, IsRanged: false } &&
                                    Target.MainHandWeapon != null && Target.MainHandWeapon.gameObject.activeInHierarchy;
            bool offHandSpearOrFist = Target.OffHandItem is { IsFists: true } or {IsPolearm: true} && Target.OffHandWeapon != null;
            _bottomClamp.Set(isDrawingBow ? Target.Data.tppBowBottomClamp : Target.Data.tppBottomClamp);
            _bottomClamp.Update(deltaTime, BottomClampUpdateSpeed);
            
            float pitch = Target.VHeroController.HeroCamera.CinemachineTargetPitch;
            pitch = GeneralUtils.ClampAngle(pitch, _bottomClamp.Value, Target.Data.tppTopClamp);
            _generalData->cameraPitch = pitch;

            
            float desiredIkStrength = Target.IsInCombat() || isDrawingBow || _isProcessingAnimationSpeed 
                ? FullyActiveIkWeight
                : OutOfFightIkWeight;
            _generalData->ikStrength = math.lerp(_generalData->ikStrength, desiredIkStrength, deltaTime * WeightLerpSpeed);
            
            for (uint i = 0; i < _spineData.Length; i++) {
                var data = _spineData[i];
                float desiredWeight = data.constraint switch {
                    SpineIKConstraint.BowAimOnly => isDrawingBow ? FullyActiveIkWeight : DisabledIkWeight,
                    SpineIKConstraint.TwoHandedOnly => isUsingTwoHanded ? FullyActiveIkWeight : DisabledIkWeight,
                    SpineIKConstraint.OffHandSpearOrFist => offHandSpearOrFist ? FullyActiveIkWeight : DisabledIkWeight,
                    _ => FullyActiveIkWeight
                };
                data.baseWeight = math.lerp(data.baseWeight, desiredWeight, deltaTime * WeightLerpSpeed);
                _spineData[i] = data;
            }
        }
        
        void Dispose() {
            if (!_created) {
                return;
            }
            
            AnimancerUtilities.RemovePlayable(_jobPlayable);
            UnsafeUtility.Free(_generalData, Allocator.Persistent);
            _spineData.Dispose();
            _transformsData.Dispose();
            Target.GetTimeDependent()?.WithoutLateUpdate(OnLateUpdate);

            _created = false;
        }
        
        protected override void OnDiscard() {
            Dispose();
        }
    }
    
    [Serializable]
    struct SpineRotationSetup {
        public Transform transformToRotate;
        [Range(-2f, 2f), FormerlySerializedAs("weight")] public float weightX;
        [Range(-2f, 2f)] public float weightY;
        [Range(-2f, 2f)] public float weightZ;
        public SpineIKConstraint constraint;
    }
}