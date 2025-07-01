using Awaken.ECS.Components;
using Awaken.ECS.DrakeRenderer;
using Awaken.Utility.Collections;
using Awaken.Utility.LowLevel.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Jobs;

namespace Awaken.ECS.Critters {
    [UpdateInGroup(typeof(CrittersSystemGroup))]
    [BurstCompile]
    public partial class CrittersTransformsSyncSystem : SystemBase {

        NativeList<CrittersGroupData> _allCritterGroupsData;
        ComponentLookup<DrakeVisualEntitiesTransform> _transformDataLookup;
        ComponentLookup<CulledEntityTag> _culledEntityTagLookup;

        protected override void OnCreate() {
            _allCritterGroupsData = new(4, ARAlloc.Persistent);
            _culledEntityTagLookup = SystemAPI.GetComponentLookup<CulledEntityTag>(true);
            _transformDataLookup = SystemAPI.GetComponentLookup<DrakeVisualEntitiesTransform>(true);
        }

        protected override void OnDestroy() {
            _allCritterGroupsData.Dispose();
        }

        protected override void OnUpdate() {
            ref var state = ref CheckedStateRef;
            _culledEntityTagLookup.Update(ref state);
            int crittersGroupsCount = _allCritterGroupsData.Length;
            for (int i = 0; i < crittersGroupsCount; i++) {
                var data = _allCritterGroupsData[i];
                if (_culledEntityTagLookup.HasComponent(data.crittersGroupEntity)) {
                    continue;
                }
                _transformDataLookup.Update(ref state);
                Dependency = new SyncTransformsJob() {
                    critterEntities = data.critterEntities,
                    transformDataLookup = _transformDataLookup
                }.Schedule(data.crittersTransforms, Dependency);
            }
        }

        public void AddCrittersGroupData(CrittersGroupData data) {
            Dependency.Complete();
            _allCritterGroupsData.Add(data);
        }

        public void RemoveCritterGroupData(Entity crittersGroupEntity) {
            Dependency.Complete();
            for (int i = 0; i < _allCritterGroupsData.Length; i++) {
                if (_allCritterGroupsData[i].crittersGroupEntity == crittersGroupEntity) {
                    _allCritterGroupsData.RemoveAtSwapBack(i);
                    return;
                }
            }
        }

        [BurstCompile]
        public struct SyncTransformsJob : IJobParallelForTransform {
            [ReadOnly] public UnsafeArray<Entity>.Span critterEntities;
            [ReadOnly] public ComponentLookup<DrakeVisualEntitiesTransform> transformDataLookup;

            public void Execute(int index, TransformAccess transform) {
                if (transformDataLookup.TryGetComponent(critterEntities[(uint)index], out var data)) {
                    transform.SetPositionAndRotation(data.position, data.rotation);
                }
            }
        }

        public struct CrittersGroupData {
            public UnsafeArray<Entity>.Span critterEntities;
            public TransformAccessArray crittersTransforms;
            public Entity crittersGroupEntity;

            public CrittersGroupData(UnsafeArray<Entity>.Span critterEntities, TransformAccessArray crittersTransforms, Entity crittersGroupEntity) {
                this.critterEntities = critterEntities;
                this.crittersTransforms = crittersTransforms;
                this.crittersGroupEntity = crittersGroupEntity;
            }
        }
    }
}