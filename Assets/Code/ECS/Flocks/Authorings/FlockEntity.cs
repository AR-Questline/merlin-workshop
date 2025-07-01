using Awaken.CommonInterfaces;
using Awaken.ECS.Authoring;
using Awaken.ECS.Authoring.LinkedEntities;
using Awaken.ECS.Components;
using Awaken.ECS.DrakeRenderer;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Sirenix.OdinInspector;
using TAO.VertexAnimation;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.ECS.Flocks.Authorings {
    public class FlockEntity : MonoBehaviour, IDrakeMeshRendererBakingStep,
        IDrakeMeshRendererBakingModificationStep, IDrakeLODBakingModificationStep, IDrakeLODFinishBakingListener {
        static ComponentType[] s_flockEntityComponentTypes;
        public static ComponentType[] FlockEntityComponentTypes => s_flockEntityComponentTypes ??= new[] {
            ComponentType.ReadWrite<DrakeVisualEntitiesTransform>(),
            ComponentType.ReadWrite<FlockAnimatorParams>(),
            ComponentType.ReadWrite<TargetParams>(),
            ComponentType.ReadWrite<MovementParams>(),
            ComponentType.ReadWrite<MovementStaticParams>(),
            ComponentType.ReadWrite<AvoidanceColliderData>(),
            ComponentType.ReadWrite<FlyingFlockEntityAnimationsData>(),
            ComponentType.ReadWrite<AvoidanceData>(),
            ComponentType.ReadWrite<CurrentMovementVector>(),
            ComponentType.ReadWrite<ReachDistanceToTarget>(),
            ComponentType.ReadWrite<FlockGroupEntity>(),
            ComponentType.ReadWrite<DrakeVisualEntity>(),
            ComponentType.ReadWrite<FlyingFlockEntityState>(),
            ComponentType.ReadOnly<FlockSoundsData>(),
            // LinkedLifetimeRequest component should always be last in the array
            ComponentType.ReadWrite<LinkedEntitiesAccessRequest>(),
        };
        
        [SerializeField, Required("Required if in scene", InfoMessageType.Warning)] FlockGroup flockGroup;
        
        [UnityEngine.Scripting.Preserve] public FlockGroup FlockGroup => flockGroup;
        public Entity Entity { get; private set; }

        DynamicBuffer<DrakeVisualEntity> _visualEntitiesBuffer;
        float _flockEntityScale = 1;
        byte _finishedStartOrFinishedDrakeLodBaking;
        
        void Awake() {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity = entityManager.CreateEntity(FlockEntityComponentTypes);

            var linkedEntityLifetime = LinkedEntityLifetime.GetOrCreate(gameObject);
            entityManager.SetComponentData(Entity, new LinkedEntitiesAccessRequest(linkedEntityLifetime));
            if (flockGroup != null) {
                SetRandomScale(flockGroup.flockEntityMinMaxScale);
            }
        }

        void Start() {
            if (flockGroup != null) {
                var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                SetupFromFlockGroup(Entity, new DrakeVisualEntitiesTransform(transform.position, transform.rotation, _flockEntityScale), flockGroup, entityManager);
            }
            _finishedStartOrFinishedDrakeLodBaking++;
            if (_finishedStartOrFinishedDrakeLodBaking > 1) {
                Destroy(this);
            }
        }
        
        public void SetRandomScale(float2 minMaxScale) {
            var random = new Unity.Mathematics.Random(math.hash(new int2(Entity.Index, Entity.Version)));
            if (flockGroup != null) {
                _flockEntityScale = random.NextFloat(minMaxScale.x, minMaxScale.y);
            }
        }
        
        public static void SetupFromFlockGroup(Entity flockEntity, DrakeVisualEntitiesTransform drakeVisualEntitiesTransform, FlockGroup flockGroup, EntityManager entityManager) {
            var targetParams = new TargetParams();
            targetParams.flockTargetPosition = flockGroup.InitialPosition;
            entityManager.SetComponentData(flockEntity, targetParams);
            entityManager.SetComponentData(flockEntity, drakeVisualEntitiesTransform);
            entityManager.SetComponentData(flockEntity, flockGroup.MovementParams);
            entityManager.SetComponentData(flockEntity, flockGroup.MovementStaticParams);
            entityManager.SetComponentData(flockEntity, flockGroup.AvoidanceColliderData);
            entityManager.SetComponentData(flockEntity, flockGroup.animationsData);
            entityManager.SetSharedComponent(flockEntity, new FlockGroupEntity(flockGroup.FlockGroupEntity));
            entityManager.SetSharedComponent(flockEntity, flockGroup.SoundsData);
        }

        public void AddComponentsDrakeRendererEntity(DrakeMeshRenderer drakeMeshRenderer, Entity lodGroupEntity, in LodGroupSerializableData lodGroupData,
            in DrakeMeshMaterialComponent drakeMeshMaterialComponent, Entity entity, ref EntityCommandBuffer ecb) {
#if UNITY_EDITOR
            if (Application.isPlaying == false) {
                return;
            }
#endif
            var visualEntitiesBuffer = _visualEntitiesBuffer.IsCreated ?
                _visualEntitiesBuffer :
                _visualEntitiesBuffer = ecb.SetBuffer<DrakeVisualEntity>(Entity);
            visualEntitiesBuffer.Add(new DrakeVisualEntity(entity));
        }

        public void ModifyDrakeLODGroup(DrakeLodGroup drakeLodGroup) {
            gameObject.isStatic = true;

            var options = new IWithUnityRepresentation.Options {
                movable = false
            };

            drakeLodGroup.SetUnityRepresentation(options);
        }

        public void ModifyDrakeMeshRenderer(DrakeMeshRenderer drakeMeshRenderer) {
            gameObject.isStatic = true;

            var options = new IWithUnityRepresentation.Options {
                movable = false
            };

            drakeMeshRenderer.SetUnityRepresentation(options);
#if UNITY_EDITOR
            if (Application.isPlaying == false) {
                return;
            }
#endif
            drakeMeshRenderer.transform.localScale = new float3(_flockEntityScale);
            if (_flockEntityScale > 1) {
                var aabb = drakeMeshRenderer.AABB;
                aabb.Extents *= _flockEntityScale;
                drakeMeshRenderer.EnsureBakingAABBExtents(aabb.Extents);
            }
        }

        public void OnDrakeLodGroupBakingFinished() {
#if UNITY_EDITOR
            if (Application.isPlaying == false) {
                return;
            }
#endif
            _finishedStartOrFinishedDrakeLodBaking++;
            if (_finishedStartOrFinishedDrakeLodBaking > 1) { 
                Destroy(this);
            }
        }

#if UNITY_EDITOR
        void OnValidate() {
            gameObject.isStatic = true;
        }
#endif
    }
}