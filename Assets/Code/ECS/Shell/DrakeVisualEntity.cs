using System;
using Unity.Entities;

namespace Awaken.ECS.DrakeRenderer {
    public struct DrakeVisualEntity : IBufferElementData {
        public Entity value;

        public DrakeVisualEntity(Entity value) {
            throw new NotImplementedException();
        }

        public static implicit operator Entity(DrakeVisualEntity value) => throw new NotImplementedException();
    }
}