using System;
using Unity.Entities;

namespace Awaken.ECS.Authoring {
    [Serializable]
    public struct DebugEntityView {
        public int index;
        public int version;

        public Entity Entity => new Entity {
            Index = index,
            Version = version
        };

        public DebugEntityView(Entity entity) {
            index = entity.Index;
            version = entity.Version;
        }

        public override string ToString() {
            return World.DefaultGameObjectInjectionWorld.EntityManager.GetName(Entity);
        }

        public static implicit operator Entity(DebugEntityView view) {
            return view.Entity;
        }
    }
}
