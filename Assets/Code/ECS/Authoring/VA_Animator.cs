using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using TAO.VertexAnimation;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Awaken.ECS.Authoring {
    public class VA_Animator : MonoBehaviour, IDrakeMeshRendererBakingStep, IDrakeLODFinishBakingListener {
        [SerializeField] bool allowControlFromManagedWorld = true;

        [SerializeField, ShowIf(nameof(allowControlFromManagedWorld))]
        VA_AnimationBook animationBook;

        [SerializeField, ShowIf(nameof(allowControlFromManagedWorld))]
        bool useAutoIdleAnimation;

        [SerializeField, ShowIf("@allowControlFromManagedWorld && useAutoIdleAnimation")]
        float autoIdleAnimSpeedThreshold = 0.1f;

        [SerializeField, ShowIf("@allowControlFromManagedWorld && useAutoIdleAnimation")]
        float autoIdleAnimSpeed = 1f;

        [SerializeField, ShowIf("@allowControlFromManagedWorld && useAutoIdleAnimation"), OnValueChanged(nameof(ValidateAutoIdleAnimation))]
        VA_Animation autoIdleAnimation;
        bool IsPlaymode => Application.isPlaying;

        byte _beforeAutoIdleAnimationIndex = byte.MaxValue;
        EntityManager _entityManager;
        Entity _animatedEntitiesBufferEntity;
        NativeArray<Entity> _animatedEntities;
        DynamicBuffer<AnimatedEntity> _animatedEntitiesBuffer;

        bool HasAnimatedEntities => _animatedEntities.IsCreated || TryRetrieveAnimatedEntities();

        void Awake() {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (allowControlFromManagedWorld) {
                _animatedEntitiesBufferEntity = _entityManager.CreateEntity(ComponentType.ReadWrite<AnimatedEntity>());
            }
        }

        void OnDestroy() {
            if (_animatedEntities.IsCreated) {
                _animatedEntities.Dispose();
            }
        }

        [Button, ShowIf(nameof(IsPlaymode))]
        public void SetAnimationTransitionTime(float time) {
            if (!HasAnimatedEntities) {
                return;
            }

            var animatorParams = GetAnimatorParams();
            animatorParams.transitionTime = time;
            SetAnimatorParams(animatorParams);
        }

        [Button, ShowIf(nameof(IsPlaymode))]
        public void SetAnimationSpeed(float speed, bool useAutoIdleAnimation = false) {
            if (useAutoIdleAnimation) {
                TryPlayAutoIdleAnimation(speed);
            }

            var targetAnimationSpeed = useAutoIdleAnimation && IsInAutoIdleAnimation() ? autoIdleAnimSpeed : speed;
            SetTargetAnimationSpeed(targetAnimationSpeed);
        }

        [Button, ShowIf(nameof(IsPlaymode))]
        public void PlayAnimation(byte animationIndex, float animationSpeed = 1, bool tryPlayAutoIdleAnimation = false) {
            SetTargetAnimationIndex(animationIndex);
            SetAnimationSpeed(animationSpeed, tryPlayAutoIdleAnimation);
        }

        public void PlayAnimation(int animationIndex, float animationSpeed = 1, bool tryPlayAutoIdleAnimation = false) {
            PlayAnimation((byte)animationIndex, animationSpeed, tryPlayAutoIdleAnimation);
        }

        public void PlayAnimation(VA_Animation animation, float animationSpeed = 1,
            bool tryPlayAutoIdleAnimation = false) {
            PlayAnimation(GetAnimationIndex(animation), animationSpeed, tryPlayAutoIdleAnimation);
        }

        public void PlayAnimation(string animationName, float animationSpeed = 1,
            bool tryPlayAutoIdleAnimation = false) {
            PlayAnimation(GetAnimationIndex(animationName), animationSpeed, tryPlayAutoIdleAnimation);
        }

        byte GetAnimationIndex(VA_Animation animation) {
            int index = animationBook.animations.IndexOf(animation);
            if (index == -1) {
                return byte.MaxValue;
            }

            return (byte)index;
        }

        byte GetAnimationIndex(string searchedAnimationName) {
            var animations = animationBook.animations;
            int count = animations.Count;
            for (int i = 0; i < count; i++) {
                var anim = animations[i];
                if (anim == null) {
                    continue;
                }

                if (anim.name == searchedAnimationName) {
                    return (byte)i;
                }
            }

            return byte.MaxValue;
        }

        public void AddComponentsDrakeRendererEntity(DrakeMeshRenderer drakeMeshRenderer, Entity lodGroupEntity, in LodGroupSerializableData lodGroupData,
            in DrakeMeshMaterialComponent drakeMeshMaterialComponent,
            Entity entity, ref EntityCommandBuffer ecb) {
            bool hasLodGroup = lodGroupEntity != Entity.Null;
            if (!hasLodGroup || IsValidLODGroup(drakeMeshRenderer)) {
                AddAnimatedEntity(entity, ecb);

                VA_Animator.AddEcsComponents(animationBook, entity, ecb);
            }
        }

        public void OnDrakeLodGroupBakingFinished() {
#if UNITY_EDITOR
            if (Application.isPlaying == false) {
                return;
            }
#endif
            if (allowControlFromManagedWorld == false) {
                Destroy(this);
            }
        }

        void AddAnimatedEntity(Entity entity, EntityCommandBuffer ecb) {
#if UNITY_EDITOR
            if (Application.isPlaying == false) {
                return;
            }
#endif
            if (allowControlFromManagedWorld == false) {
                return;
            }

            if (_animatedEntitiesBufferEntity == Entity.Null) {
                Debug.LogError($"Trying to attach entity {entity.ToString()} to {nameof(VA_Animator)} after already added entities with other command buffer. This is not allowed");
                return;
            }

            if (_animatedEntitiesBuffer.IsCreated == false) {
                _animatedEntitiesBuffer = ecb.SetBuffer<AnimatedEntity>(_animatedEntitiesBufferEntity);
            }

            _animatedEntitiesBuffer.Add(entity);
        }

        static bool IsValidLODGroup(DrakeMeshRenderer drakeMeshRenderer) {
            return (drakeMeshRenderer.TryGetComponent(out VA_AnimationLOD animationLOD) &&
                    LODRange.GetLODIndex(drakeMeshRenderer.LodMask) <= animationLOD.LastLODWithAnimation);
        }

        void TryPlayAutoIdleAnimation(float speed) {
            if (!HasAnimatedEntities) {
                return;
            }

            bool isInAutoIdleAnim = IsInAutoIdleAnimation();
            if (!isInAutoIdleAnim && useAutoIdleAnimation && speed <= autoIdleAnimSpeedThreshold) {
                if (autoIdleAnimation == null) {
                    Debug.LogError($"{nameof(autoIdleAnimation)} is not set on GameObject {gameObject.name}");
                    return;
                }

                _beforeAutoIdleAnimationIndex = GetAnimatorParams().targetAnimationIndex;
                PlayAnimation(autoIdleAnimation, autoIdleAnimSpeed, tryPlayAutoIdleAnimation: false);
            } else if (isInAutoIdleAnim && useAutoIdleAnimation && speed > autoIdleAnimSpeedThreshold) {
                PlayAnimation(_beforeAutoIdleAnimationIndex, speed, tryPlayAutoIdleAnimation: false);
                _beforeAutoIdleAnimationIndex = byte.MaxValue;
            }
        }

        bool IsInAutoIdleAnimation() => _beforeAutoIdleAnimationIndex != byte.MaxValue;

        void ValidateAutoIdleAnimation() {
#if UNITY_EDITOR
            if (animationBook.animations.IndexOf(autoIdleAnimation) == -1) {
                Debug.LogError($"{nameof(autoIdleAnimation)} should be an animation from {nameof(animationBook)}");
                autoIdleAnimation = null;
            }
#endif
        }

        void SetAnimatorParams(VA_AnimatorParams animatorParams) {
            for (int i = 0; i < _animatedEntities.Length; i++) {
                _entityManager.SetComponentData(_animatedEntities[i], animatorParams);
            }
        }

        VA_AnimatorParams GetAnimatorParams() {
            return _entityManager.GetComponentData<VA_AnimatorParams>(_animatedEntities[0]);
        }

        bool TryRetrieveAnimatedEntities() {
            if (allowControlFromManagedWorld == false) {
                Log.Important?.Error($"Cannot control {nameof(VA_Animator)} {gameObject.name} from managed world");
                return false;
            }

            var animatedEntities = _entityManager.GetBuffer<AnimatedEntity>(_animatedEntitiesBufferEntity).Reinterpret<Entity>().AsNativeArray();
            if (animatedEntities.Length > 0 && animatedEntities[0].Index >= 0) {
                _animatedEntities = animatedEntities.CreateCopy(Allocator.Persistent);
                _entityManager.DestroyEntity(_animatedEntitiesBufferEntity);
                _animatedEntitiesBufferEntity = Entity.Null;
                _animatedEntitiesBuffer = default;
                return true;
            }

            return false;
        }

        void SetTargetAnimationIndex(byte targetAnimationIndex) {
            if (!HasAnimatedEntities) {
                return;
            }

            var animatorParams = GetAnimatorParams();
            animatorParams.targetAnimationIndex = targetAnimationIndex;
            SetAnimatorParams(animatorParams);
        }

        void SetTargetAnimationSpeed(float targetAnimationSpeed) {
            if (!HasAnimatedEntities) {
                return;
            }

            var animatorParams = GetAnimatorParams();
            animatorParams.targetAnimationSpeed = targetAnimationSpeed;
            SetAnimatorParams(animatorParams);
        }

        public static void AddEcsComponents(VA_AnimationBook animationBookSO, VA_AnimatorParams animatorParamsCom,
            Entity entity,
            EntityManager entityManager) {
            var animBookAssetRef = VA_AnimationBookBlobRef.GetOrCreateBlobRef(animationBookSO, entityManager);
            VA_CurrentAnimationData currentAnimationData = VA_CurrentAnimationData.Default;
            entityManager.AddComponentData(entity, currentAnimationData);
            entityManager.AddComponentData(entity, animatorParamsCom);
            entityManager.AddComponentData(entity,
                new VA_SharedAnimationData(animBookAssetRef));
            entityManager.AddComponent<VA_AnimationMaterialPropertyData>(entity);
            entityManager.AddComponent<VA_AnimationLerpMaterialPropertyData>(entity);
        }

        public static void AddEcsComponents(VA_AnimationBook animationBookSO,
            Entity entity,
            EntityCommandBuffer ecb) {
            var animBookAssetRef = VA_AnimationBookBlobRef.GetOrCreateBlobRef(animationBookSO, ecb);
            VA_CurrentAnimationData currentAnimationData = VA_CurrentAnimationData.Default;
            ecb.AddComponent(entity, currentAnimationData);
            ecb.AddComponent(entity, new VA_AnimatorParams());
            ecb.AddComponent(entity, new VA_SharedAnimationData(animBookAssetRef));
            ecb.AddComponent<VA_AnimationMaterialPropertyData>(entity);
            ecb.AddComponent<VA_AnimationLerpMaterialPropertyData>(entity);
        }

        [InternalBufferCapacity(0)]
        struct AnimatedEntity : IBufferElementData {
            public Entity value;
            public static implicit operator AnimatedEntity(Entity entity) => new() { value = entity };
        }
    }
}