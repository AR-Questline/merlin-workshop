using System.Collections.Generic;
using Unity.Entities;

namespace TAO.VertexAnimation {
    public class VA_AnimationBookBlobRef : IComponentData {
        public BlobAssetReference<VA_AnimationBookData> AnimationBookBlobRef;

        static Dictionary<VA_AnimationBook, BlobAssetReference<VA_AnimationBookData>> bookSOToBlob = new();

        public static void Clear() {
            bookSOToBlob.Clear();
        }

        public static BlobAssetReference<VA_AnimationBookData> GetOrCreateBlobRef(VA_AnimationBook bookSO,
            EntityManager entityManager) {
            if (bookSOToBlob.TryGetValue(bookSO, out var bookBlobRef)) {
                return bookBlobRef;
            }
            return CreateAndRegisterBookBlobRef(bookSO, entityManager);
        }
        
        public static BlobAssetReference<VA_AnimationBookData> GetOrCreateBlobRef(VA_AnimationBook bookSO,
            EntityCommandBuffer ecb) {
            if (bookSOToBlob.TryGetValue(bookSO, out var bookBlobRef)) {
                return bookBlobRef;
            }
            return CreateAndRegisterBookBlobRef(bookSO, ecb);
        }


        static BlobAssetReference<VA_AnimationBookData> CreateAndRegisterBookBlobRef(VA_AnimationBook bookSO,
            EntityCommandBuffer ecb) {
            var entity = ecb.CreateEntity();
            var bookBlobRef = bookSO.GetBlobAssetRef();
            ecb.AddComponent(entity, new VA_AnimationBookBlobRef() {
                AnimationBookBlobRef = bookBlobRef
            });
            bookSOToBlob[bookSO] = bookBlobRef;
            return bookBlobRef;
        }
        static BlobAssetReference<VA_AnimationBookData> CreateAndRegisterBookBlobRef(VA_AnimationBook bookSO,
            EntityManager entityManager) {
            var entity = entityManager.CreateEntity(ComponentType.ReadWrite<VA_AnimationBookBlobRef>());
            var bookBlobRef = bookSO.GetBlobAssetRef();
            entityManager.SetComponentData(entity, new VA_AnimationBookBlobRef() {
                AnimationBookBlobRef = bookBlobRef
            });
            bookSOToBlob[bookSO] = bookBlobRef;
            return bookBlobRef;
        }
    }
}