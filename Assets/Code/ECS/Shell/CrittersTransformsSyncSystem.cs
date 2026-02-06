using System;
using Awaken.ECS.DrakeRenderer;
using Awaken.Utility.LowLevel.Collections;
using Unity.Entities;
using UnityEngine.Jobs;

namespace Awaken.ECS.Critters {
    public partial class CrittersTransformsSyncSystem : SystemBase {
        protected override void OnCreate() {
            throw new NotImplementedException();
        }

        protected override void OnDestroy() {
            throw new NotImplementedException();
        }

        protected override void OnUpdate() {
            throw new NotImplementedException();
        }

        public void AddCrittersGroupData(CrittersGroupData data) {
            throw new NotImplementedException();
        }

        public void RemoveCritterGroupData(Entity crittersGroupEntity) {
            throw new NotImplementedException();
        }

        public struct SyncTransformsJob : IJobParallelForTransform {
            public UnsafeArray<Entity>.Span critterEntities;
            public ComponentLookup<DrakeVisualEntitiesTransform> transformDataLookup;

            public void Execute(int index, TransformAccess transform) {
                throw new NotImplementedException();
            }
        }

        public struct CrittersGroupData {
            public UnsafeArray<Entity>.Span critterEntities;
            public TransformAccessArray crittersTransforms;
            public Entity crittersGroupEntity;

            public CrittersGroupData(UnsafeArray<Entity>.Span critterEntities, TransformAccessArray crittersTransforms,
                Entity crittersGroupEntity) {
                throw new NotImplementedException();
            }
        }
    }
}