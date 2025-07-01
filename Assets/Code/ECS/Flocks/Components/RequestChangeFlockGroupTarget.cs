using Unity.Entities;

namespace Awaken.ECS.Flocks {
    public struct RequestChangeFlockGroupTarget : IComponentData {
        public bool value;

        public RequestChangeFlockGroupTarget(bool value) {
            this.value = value;
        }
    }
}