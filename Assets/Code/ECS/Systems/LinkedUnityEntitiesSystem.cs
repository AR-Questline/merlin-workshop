using Awaken.ECS.Components;
using Awaken.PackageUtilities.Collections;
using Awaken.Utility.Collections;
using Unity.Entities;

namespace Awaken.ECS.Systems {
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    public partial class LinkedUnityEntitiesSystem : SystemBase {
        EntityQuery _toRegisterQuery;

        protected override void OnCreate() {
            _toRegisterQuery = GetEntityQuery(typeof(LinkedEntitiesAccessRequest));
            RequireForUpdate(_toRegisterQuery);
        }

        protected override void OnUpdate() {
            var entities = _toRegisterQuery.ToEntityArray(ARAlloc.Temp);
            var requests = _toRegisterQuery.ToComponentDataArray<LinkedEntitiesAccessRequest>(ARAlloc.Temp);

            var entitiesToRequest = new ARUnsafeList<Entity>(24, ARAlloc.Temp);
            for (var i = entities.Length - 1; i >= 0; --i) {
                var entity = entities[i];
                var request = requests[i];

                entitiesToRequest.Add(entity);

                var id = request.GetHashCode();

                for (int j = i - 1; j >= 0; --j) {
                    if (requests[j].GetHashCode() == id) {
                        entitiesToRequest.Add(entities[j]);
                        --i;

                        entities[j] = entities[i];
                        requests[j] = requests[i];
                    }
                }

                // There is possibility that in the same frame we spawn and destroy access so we need to check if the access is still valid
                if (request.linkedEntitiesAccessRef.IsValid()) {
                    request.linkedEntitiesAccessRef.Value.Link(entitiesToRequest.AsUnsafeSpan());
                } else {
                    if (request.destroyIfLinkInvalid) {
                        EntityManager.DestroyEntity(entity);
                    }
                }
                entitiesToRequest.Clear();
            }
            entitiesToRequest.Dispose();

            EntityManager.RemoveComponent<LinkedEntitiesAccessRequest>(_toRegisterQuery);

            entities.Dispose();
            requests.Dispose();
        }
    }
}
