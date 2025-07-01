#if ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER) && UNITY_EDITOR

using Unity.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Hash128 = UnityEngine.Hash128;
using UnityEditor;

namespace Unity.Rendering.Occlusion.Masked.Dots
{
    class OccluderBaker : Baker<Occluder>
    {
        public override void Bake(Occluder authoring)
        {
            if (IsActive() && authoring.mesh != null)
            {
                // This tells the baker API to create a dependency. If the referenced mesh changes, then baking will be
                // re-triggered.
                Mesh mesh = DependsOn(authoring.mesh);

                // Add the occluder mesh component to each submesh. This involves copying the submesh's triangle data to
                // the new component.
                if (mesh.subMeshCount > 1)
                {
                    for (int i = 0; i < authoring.mesh.subMeshCount; i++)
                    {
                        Entity entity = CreateAdditionalEntity(TransformUsageFlags.Dynamic);
                        AddOccluderComponent(entity, authoring, i);
                    }
                }
                else
                {
                    Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                    AddOccluderComponent(entity, authoring, 0);
                }

            }
        }

        private unsafe void AddOccluderComponent(Entity entity, Occluder occluder, int submeshIndex)
        {
            var component = new OcclusionMesh();

            // Get fast zero-copy access to raw mesh data
            using var meshDataArray = MeshUtility.AcquireReadOnlyMeshData(occluder.mesh);
            // Since we passed in only one mesh to `Mesh.AcquireReadOnlyMeshData()`, the array is guaranteed to have
            // only one mesh data.
            Assert.IsTrue(meshDataArray.Length == 1);
            var meshData = meshDataArray[0];


            // Each occluder component references a blob asset containing the vertex data, and another one containing
            // index data. If multiple occluders in the scene have the same meshes, then we want to share their index
            // and vertex data. This is why we compute two hashes.
            //
            // When computing the hashes, we use the raw mesh data coupled with the sub-mesh index. When creating the
            // actual blob asset, we do some extra calculations like applying the sub-mesh's index offset. We skip these
            // calculations when computing the hash, in the interest of speed.
            Hash128 indexHash = default;
            Hash128 vertexHash = default;
            {
                // Hash the sub-mesh index only to the index data, since the vertex buffer is the same across
                // sub-meshes
                HashUtilities.ComputeHash128(ref submeshIndex, ref indexHash);

                // Hash the index buffer
                var indexHashPtr = (Hash128*) UnsafeUtility.AddressOf(ref indexHash);
                var indices = meshData.GetIndexData<byte>();
                HashUnsafeUtilities.ComputeHash128(
                    indices.GetUnsafeReadOnlyPtr(),
                    (ulong) indices.Length,
                    indexHashPtr
                );
                // Hash the vertex buffer
                var vertexHashPtr = (Hash128*) UnsafeUtility.AddressOf(ref vertexHash);
                var vertices = meshData.GetVertexData<byte>();
                HashUnsafeUtilities.ComputeHash128(
                    vertices.GetUnsafeReadOnlyPtr(),
                    (ulong) vertices.Length,
                    vertexHashPtr
                );
            }

            SubMeshDescriptor subMesh = meshData.GetSubMesh(submeshIndex);

            // Create/get a blob asset reference with the mesh's vertices, and assign it to our new component. Submeshes
            // can arbitrarily index into the mesh's vertex buffer, so instead of slowly copying out only the vertices
            // that a submesh needs, we quickly copy the whole vertex buffer.
            {
                if (!TryGetBlobAssetReference(vertexHash, out BlobAssetReference<float3> blobAssetRef))
                {
                    // ^ A blob asset with the given hash doesn't already exist, so we need to add one.
                    var vertices = new NativeArray<Vector3>(meshData.vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                    meshData.GetVertices(vertices);

                    // Vector3 arrays can be directly reinterpreted as float3 arrays. If we weren't getting a pointer,
                    // we could do: `NativeArray<float3> verticesFloat3s = vertices.Reinterpret<Vector3, float3>();`
                    // The pointer makes this unnecessary.
                    void* verticesPtr = vertices.GetUnsafeReadOnlyPtr();

                    blobAssetRef = BlobAssetReference<float3>.Create(
                        verticesPtr,
                        sizeof(float3) * vertices.Length
                    );

                    vertices.Dispose();
                    AddBlobAssetWithCustomHash(ref blobAssetRef, vertexHash);
                }

                component.vertexCount = meshData.vertexCount;
                component.vertexData = blobAssetRef;
            }

            // Create a blob asset reference with the submesh's indices, and assign it to our new component
            {
                if (!TryGetBlobAssetReference(indexHash, out BlobAssetReference<int> blobAssetRef))
                {
                    // ^ A blob asset with the given hash doesn't already exist, so we need to add one.
                    var indices = new NativeArray<int>(subMesh.indexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                    meshData.GetIndices(indices, submeshIndex, false);

                    blobAssetRef = BlobAssetReference<int>.Create(
                        indices.GetUnsafeReadOnlyPtr(),
                        sizeof(int) * indices.Length
                    );

                    indices.Dispose();
                    AddBlobAssetWithCustomHash(ref blobAssetRef, indexHash);
                }

                component.indexCount = subMesh.indexCount;
                component.indexData = blobAssetRef;
            }

            // Set the transform of the occluder
            {
                // Compute the full 4x4 matrix. The last row will always be (0, 0, 0, 1). We discard this row to reduce
                // memory bandwidth and then reconstruct it later while transforming the occluders.
                float4x4 mtx = float4x4.TRS(occluder.localPosition, occluder.localRotation, occluder.localScale);
                component.localTransform = new float3x4(mtx.c0.xyz, mtx.c1.xyz, mtx.c2.xyz, mtx.c3.xyz);
            }

            // Add the component to the entity
            AddComponent(entity, component);
        }
    }
}

#endif
