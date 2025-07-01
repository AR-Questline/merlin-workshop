using Unity.Entities;

namespace TAO.VertexAnimation {
    public struct VA_SharedAnimationData : IComponentData {
        public BlobAssetReference<VA_AnimationBookData> AnimationsDatasRef;
        
        public VA_SharedAnimationData(BlobAssetReference<VA_AnimationBookData> animationsDatasRef) {
            AnimationsDatasRef = animationsDatasRef;
        }
    }
}