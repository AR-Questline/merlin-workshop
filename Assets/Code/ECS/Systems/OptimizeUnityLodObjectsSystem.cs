using Awaken.ECS.Components;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

namespace Awaken.ECS.Systems {
    [RequireMatchingQueriesForUpdate]
    public partial class OptimizeUnityLodObjectsSystem : SystemBase {
        EntityQuery _lodGroupRemoveQuery;

        EntityQuery _lodGroupJustOptimizeQuery;
        ComponentTypeSet _lodGroupToRemoveSet;

        EntityQuery _lodElementQuery;
        ComponentTypeSet _lodElementToRemoveSet;

        protected override void OnCreate() {
            _lodGroupRemoveQuery = GetEntityQuery(
                new EntityQueryDesc {
                    All = new ComponentType[] { typeof(Static), typeof(LodGroupToOptimizeAndRemoveTag), typeof(MeshLODGroupComponent), typeof(LODGroupWorldReferencePoint) }
                });

            _lodGroupJustOptimizeQuery = GetEntityQuery(
                new EntityQueryDesc {
                    All = new ComponentType[] { typeof(Static), typeof(LodGroupToOptimizeTag), typeof(MeshLODGroupComponent), typeof(LODGroupWorldReferencePoint) }
                });

            _lodGroupToRemoveSet = new(typeof(MeshLODGroupComponent), typeof(LodGroupToOptimizeTag), typeof(LODGroupWorldReferencePoint));

            _lodElementQuery = GetEntityQuery(
                new EntityQueryDesc {
                    All = new ComponentType[] { typeof(Static), typeof(MeshLODComponent), typeof(LODRange), typeof(LodMeshToOptimizeTag) }
                });

            _lodElementToRemoveSet = new(typeof(MeshLODComponent), typeof(LodMeshToOptimizeTag));
        }

        protected override void OnUpdate() {
            EntityManager.DestroyEntity(_lodGroupRemoveQuery);
            EntityManager.RemoveComponent(_lodGroupJustOptimizeQuery, _lodGroupToRemoveSet);
            EntityManager.RemoveComponent(_lodElementQuery, _lodElementToRemoveSet);
        }
    }
}
