using System;
using Unity.Entities;

namespace Awaken.ECS.Critters.Components {
    public struct CritterIndexInGroup : IComponentData {
        public int value;

        public CritterIndexInGroup(int value) {
            throw new NotImplementedException();
        }
    }
}