using Unity.Entities;
using Unity.Collections;

namespace TAO.VertexAnimation {
    /*public struct VA_AnimationLibrarySingleton : IComponentData {
        public BlobAssetReference<VA_AnimationBookData> Data;
    }

    [UnityEngine.DisallowMultipleComponent]
    public class VA_AnimationLibraryComponentAuthoring : UnityEngine.MonoBehaviour {
        public VA_AnimationLibrary AnimationBook;
        public bool debugMode = false;

        public class Baker : Baker<VA_AnimationLibraryComponentAuthoring> {
            public override void Bake(VA_AnimationLibraryComponentAuthoring authoring) {
                if (authoring.AnimationBook == null) {
                    return;
                }
                authoring.AnimationBook.GetBlobAssetRef(out var animLibAssetRef, out var animLibAssetHash);
                // Add it to the asset store.
                AddBlobAssetWithCustomHash(ref animLibAssetRef, animLibAssetHash);

                if (authoring.debugMode) {
                    UnityEngine.Debug.Log("VA_AnimationLibrary has " +
                                          animLibAssetRef.Value.animations.Length.ToString() + " animations.");
                }

                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new VA_AnimationLibrarySingleton() { Data = animLibAssetRef });
            }
        }
    }*/
}