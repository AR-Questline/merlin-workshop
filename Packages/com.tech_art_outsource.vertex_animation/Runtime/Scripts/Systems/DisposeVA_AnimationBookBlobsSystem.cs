using Unity.Entities;

namespace TAO.VertexAnimation {
    [UpdateInGroup(typeof(VertexAnimationSystemGroup), OrderLast = true)]
    public partial class DisposeVA_AnimationBookBlobsSystem : SystemBase {
        protected override void OnUpdate() { }

        protected override void OnDestroy() {
            base.OnDestroy();
            foreach (var blobRef in SystemAPI.Query<VA_AnimationBookBlobRef>()) {
                blobRef.AnimationBookBlobRef.Dispose();
            }

            VA_AnimationBookBlobRef.Clear();
        }
    }
}