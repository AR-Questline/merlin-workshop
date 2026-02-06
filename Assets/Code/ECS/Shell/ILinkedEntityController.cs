using Awaken.Utility.LowLevel.Collections;
using Unity.Entities;

namespace Awaken.ECS.Authoring.LinkedEntities {
    public interface ILinkedEntityController {
        void OnAddedEntities(in UnsafeArray<Entity>.Span linkedEntities);
        void OnDestroyUnity(in UnsafeArray<Entity> linkedEntities);
    }
}