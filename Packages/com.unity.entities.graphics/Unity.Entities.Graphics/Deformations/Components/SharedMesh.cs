using Unity.Core;
using Unity.Entities;

namespace Unity.Rendering
{
    internal struct SharedMeshTracker : ICleanupComponentData
    {
        public int VersionHash;
    }

    internal struct SharedMeshData
    {
        public UnityEngine.Rendering.BatchMeshID MeshID;

        public int VertexCount;
        public int BlendShapeCount;
        public int BoneCount;
        public int RefCount;

        public bool HasSkinning => BoneCount > 0;
        public bool HasBlendShapes => BlendShapeCount > 0;

        public readonly int StateHash()
        {
            int hash = 0;
            unsafe
            {
                var buffer = stackalloc int[]
                {
                    (int)MeshID.value,
                    VertexCount,
                    BlendShapeCount,
                    BoneCount,
                };

                hash = (int)XXHash.Hash32((byte*)buffer, 4 * 4);
            }

            return hash;
        }
    }
}
