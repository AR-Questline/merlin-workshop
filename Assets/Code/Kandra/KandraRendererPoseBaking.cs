using Awaken.Kandra.Data;
using Awaken.PackageUtilities.Collections;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Rendering;
using UniversalProfiling;

namespace Awaken.Kandra {
    public static class KandraRendererPoseBaking {
        static readonly UniversalProfilerMarker BakePoseVerticesMarker = new UniversalProfilerMarker("BakePoseVertices");
        static readonly UniversalProfilerMarker BakePoseMeshMarker = new UniversalProfilerMarker("BakePoseMesh");
        static readonly UniversalProfilerMarker UpdatePoseMeshMarker = new UniversalProfilerMarker("UpdatePoseMesh");

        public static (UnsafeArray<CompressedVertex>, UnsafeArray<AdditionalVertexData>) BakePoseVertices(this KandraRenderer kandraRenderer, Allocator allocator) {
            return kandraRenderer.BakePoseVertices(allocator, float4x4.identity.orthonormal());
        }

        public static (UnsafeArray<CompressedVertex>, UnsafeArray<AdditionalVertexData>) BakePoseVertices(this KandraRenderer kandraRenderer, Allocator allocator, float3x4 world2DesiredSpace) {
            var (handle, taa) = kandraRenderer.BakePoseVertices(allocator, world2DesiredSpace, default(JobHandle), out var skinnedVertices, out var additionalData);
            handle.Complete();
            taa.Dispose();

            return (skinnedVertices, additionalData);
        }

        public static unsafe (JobHandle, TransformAccessArray) BakePoseVertices(this KandraRenderer kandraRenderer, Allocator allocator, float3x4 world2DesiredSpace, JobHandle dependencies, out UnsafeArray<CompressedVertex> skinnedVertices, out UnsafeArray<AdditionalVertexData> additionalData) {
            BakePoseVerticesMarker.Begin();

            var managedRigBones = kandraRenderer.rendererData.rig.bones;
            var taa = new TransformAccessArray(managedRigBones);
            var rigBones = new UnsafeArray<Bone>((uint)managedRigBones.Length, ARAlloc.Temp);

            var sampleRigHandle = new SampleRig {
                outRigBones = rigBones
            }.ScheduleReadOnly(taa, 16, dependencies);

            var mesh = kandraRenderer.rendererData.mesh;
            var meshData = mesh.ReadSerializedData(KandraRendererManager.Instance.StreamingManager.LoadMeshData(mesh));

            var vertices = meshData.vertices;
            additionalData = meshData.additionalData.ToUnsafeArray(allocator);
            var boneWeights = meshData.boneWeights;
            var bindPoses = meshData.bindposes;

            var skinBones = new UnsafeArray<Bone>(bindPoses.Length, ARAlloc.Temp);

            var usedBones = kandraRenderer.rendererData.bones;
            var usedBonesPtr = (ushort*)UnsafeUtility.PinGCArrayAndGetDataAddress(usedBones, out var usedBonesHandle);
            var prepareBonesHandle = new PrepareBones {
                rigBones = rigBones,
                usedBones = usedBonesPtr,
                bindPoses = bindPoses,
                outSkinBones = skinBones
            }.ScheduleParallel(skinBones.LengthInt, 64, sampleRigHandle);

            var releaseUsedBonesHandle = UnsafeUtils.ReleasePinnedArray(usedBonesHandle, prepareBonesHandle);

            skinnedVertices = new UnsafeArray<CompressedVertex>(vertices.Length, allocator);
            var skinningHandle = new Skinning {
                world2Space = world2DesiredSpace,
                originalVertices = vertices,
                boneWeights = boneWeights,
                skinBones = skinBones,

                outVertices = skinnedVertices,
            }.ScheduleParallel(skinnedVertices.LengthInt, 32, prepareBonesHandle);

            var disposeBones = skinBones.Dispose(skinningHandle);
            var disposeRig = rigBones.Dispose(skinningHandle);

            BakePoseVerticesMarker.End();
            return (JobHandle.CombineDependencies(disposeBones, disposeRig, releaseUsedBonesHandle), taa);
        }

        public static Mesh BakePoseMesh(this KandraRenderer kandraRenderer) {
            return kandraRenderer.BakePoseMesh(float4x4.identity.orthonormal());
        }

        public static Mesh BakePoseMesh(this KandraRenderer kandraRenderer, float3x4 world2DesiredSpace) {
            BakePoseMeshMarker.Begin();

            var (vertices, additionalData) = kandraRenderer.BakePoseVertices(ARAlloc.Temp, world2DesiredSpace);

            var meshDataArray = Mesh.AllocateWritableMeshData(1);
            var meshData = meshDataArray[0];

            meshData.SetVertexBufferParams(vertices.LengthInt,
                new VertexAttributeDescriptor(VertexAttribute.Position),
                new VertexAttributeDescriptor(VertexAttribute.Normal),
                new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, stream: 1));

            var meshVertices = meshData.GetVertexData<FullVertex>();
            var meshUVs = meshData.GetVertexData<float2>(1);

            AllocationsTracker.CustomAllocation(meshVertices);
            AllocationsTracker.CustomAllocation(meshUVs);

            var fillMeshHandle = new FillMesh {
                skinnedVertices = vertices,
                additionalData = additionalData,

                outMeshVertices = meshVertices.AsUnsafeSpan(),
                outMeshUVs = meshUVs.AsUnsafeSpan()
            }.ScheduleParallel(vertices.LengthInt, 128, default);

            var kandraMesh = kandraRenderer.rendererData.mesh;
            var indices = KandraRendererManager.Instance.StreamingManager.LoadIndicesData(kandraMesh);
            meshData.SetIndexBufferParams(indices.LengthInt, IndexFormat.UInt16);

            var indexBuffer = meshData.GetIndexData<ushort>();
            indexBuffer.CopyFrom(indices.AsNativeArray());

            meshData.subMeshCount = kandraMesh.submeshes.Length;
            for (var i = 0; i < kandraMesh.submeshes.Length; i++) {
                var submesh = kandraMesh.submeshes[i];
                meshData.SetSubMesh(i, new SubMeshDescriptor((int)submesh.indexStart, (int)submesh.indexCount));
            }

            fillMeshHandle.Complete();

            AllocationsTracker.CustomFree(meshVertices);
            AllocationsTracker.CustomFree(meshUVs);

            var mesh = new Mesh();
#if UNITY_EDITOR
            mesh.name = $"RuntimeSkinned_{kandraRenderer.name}";
#endif
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
            mesh.RecalculateBounds();

            vertices.Dispose();
            additionalData.Dispose();

            BakePoseMeshMarker.End();

            return mesh;
        }

        public static void BakePoseMesh(this KandraRenderer kandraRenderer, Mesh mesh, float3x4 world2DesiredSpace) {
            BakePoseMeshMarker.Begin();

            var meshDataArray = Mesh.AllocateWritableMeshData(mesh);
            var meshData = meshDataArray[0];

            kandraRenderer.BakePoseMesh(meshData, world2DesiredSpace);

            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
            mesh.RecalculateBounds();

            BakePoseMeshMarker.End();
        }

        public static void BakePoseMesh(this KandraRenderer kandraRenderer, Mesh.MeshData meshData, float3x4 world2DesiredSpace) {
            BakePoseMeshMarker.Begin();

            var (vertices, additionalData) = kandraRenderer.BakePoseVertices(ARAlloc.Temp, world2DesiredSpace);

            var meshVertices = meshData.GetVertexData<FullVertex>();
            var meshUVs = meshData.GetVertexData<float2>(1);

            AllocationsTracker.CustomAllocation(meshVertices);
            AllocationsTracker.CustomAllocation(meshUVs);

            var fillMeshHandle = new FillMesh {
                skinnedVertices = vertices,
                additionalData = additionalData,

                outMeshVertices = meshVertices.AsUnsafeSpan(),
                outMeshUVs = meshUVs.AsUnsafeSpan()
            }.ScheduleParallel(vertices.LengthInt, 128, default);

            fillMeshHandle.Complete();

            AllocationsTracker.CustomFree(meshVertices);
            AllocationsTracker.CustomFree(meshUVs);

            vertices.Dispose();
            additionalData.Dispose();

            BakePoseMeshMarker.End();
        }

        public static void UpdatePoseMesh(this KandraRenderer kandraRenderer, Mesh mesh, float3x4 world2DesiredSpace) {
            UpdatePoseMeshMarker.Begin();

            var meshDataArray = Mesh.AllocateWritableMeshData(mesh);
            var meshData = meshDataArray[0];

            kandraRenderer.UpdatePoseMesh(meshData, world2DesiredSpace);

            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
            mesh.RecalculateBounds();

            UpdatePoseMeshMarker.End();
        }

        public static void UpdatePoseMesh(this KandraRenderer kandraRenderer, Mesh.MeshData meshData, float3x4 world2DesiredSpace) {
            var (handle, taa) = kandraRenderer.UpdatePoseMesh(meshData, world2DesiredSpace, default);
            handle.Complete();
            taa.Dispose();
        }

        public static (JobHandle, TransformAccessArray) UpdatePoseMesh(this KandraRenderer kandraRenderer, Mesh.MeshData meshData, float3x4 world2DesiredSpace, JobHandle dependencies) {
            UpdatePoseMeshMarker.Begin();

            var (poseVerticesHandle, taa) = kandraRenderer.BakePoseVertices(ARAlloc.Temp, world2DesiredSpace, dependencies, out var skinnedVertices, out var additionalData);

            var meshVertices = meshData.GetVertexData<FullVertex>();

            AllocationsTracker.CustomAllocation(meshVertices);

            var uploadMeshHandle = new UpdateMesh {
                skinnedVertices = skinnedVertices,
                additionalData = additionalData,

                outMeshVertices = meshVertices.AsUnsafeSpan(),
            }.ScheduleParallel(skinnedVertices.LengthInt, 128, poseVerticesHandle);

            var skinnedVerticesDispose = skinnedVertices.Dispose(uploadMeshHandle);
            var additionalDataDispose = additionalData.Dispose(uploadMeshHandle);

            var freeHandle = AllocationsTracker.CustomFreeSchedule(meshVertices, JobHandle.CombineDependencies(skinnedVerticesDispose, additionalDataDispose));

            UpdatePoseMeshMarker.End();

            return (freeHandle, taa);
        }

        public static Mesh BlankMesh(this KandraRenderer kandraRenderer) {
            var meshDataArray = Mesh.AllocateWritableMeshData(1);
            var meshData = meshDataArray[0];

            var kandraMesh = kandraRenderer.rendererData.mesh;

            meshData.SetVertexBufferParams(kandraMesh.vertexCount,
                new VertexAttributeDescriptor(VertexAttribute.Position),
                new VertexAttributeDescriptor(VertexAttribute.Normal),
                new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, stream: 1));

            var indices = KandraRendererManager.Instance.StreamingManager.LoadIndicesData(kandraMesh);
            meshData.SetIndexBufferParams(indices.LengthInt, IndexFormat.UInt16);

            var indexBuffer = meshData.GetIndexData<ushort>();
            indexBuffer.CopyFrom(indices.AsNativeArray());

            meshData.subMeshCount = kandraMesh.submeshes.Length;
            for (var i = 0; i < kandraMesh.submeshes.Length; i++) {
                var submesh = kandraMesh.submeshes[i];
                meshData.SetSubMesh(i, new SubMeshDescriptor((int)submesh.indexStart, (int)submesh.indexCount), MeshUpdateFlags.DontRecalculateBounds);
            }

            var mesh = new Mesh();
#if UNITY_EDITOR
            mesh.name = $"RuntimeSkinned_{kandraRenderer.name}";
#endif
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
            mesh.bounds = kandraMesh.meshLocalBounds;

            return mesh;
        }

        [BurstCompile]
        struct SampleRig : IJobParallelForTransform {
            public UnsafeArray<Bone> outRigBones;

            public void Execute(int index, TransformAccess transform) {
                outRigBones[(uint)index] = new Bone(transform.localToWorldMatrix);
            }
        }

        [BurstCompile]
        unsafe struct PrepareBones : IJobFor {
            [ReadOnly] public UnsafeArray<Bone> rigBones;
            [ReadOnly, NativeDisableUnsafePtrRestriction] public ushort* usedBones;
            [ReadOnly] public UnsafeArray<float3x4>.Span bindPoses;

            public UnsafeArray<Bone> outSkinBones;

            public void Execute(int index) {
                var uIndex = (uint)index;
                var bindPose = bindPoses[uIndex];
                var bone = rigBones[usedBones[uIndex]];

                outSkinBones[uIndex] = new Bone(mathExt.mul(bone.boneTransform, bindPose));
            }
        }

        [BurstCompile]
        struct Skinning : IJobFor {
            [ReadOnly] public float3x4 world2Space;
            [ReadOnly] public UnsafeArray<CompressedVertex>.Span originalVertices;
            [ReadOnly] public UnsafeArray<PackedBonesWeights>.Span boneWeights;
            [ReadOnly] public UnsafeArray<Bone>.Span skinBones;

            public UnsafeArray<CompressedVertex>.Span outVertices;

            public void Execute(int index) {
                var uIndex = (uint)index;

                var boneWeight = boneWeights[uIndex];
                float3x4 skinTransform = skinBones[boneWeight.Index0].boneTransform * boneWeight.Weight0 +
                                         skinBones[boneWeight.Index1].boneTransform * boneWeight.Weight1 +
                                         skinBones[boneWeight.Index2].boneTransform * boneWeight.Weight2 +
                                         skinBones[boneWeight.Index2].boneTransform * boneWeight.Weight3;

                skinTransform = mathExt.mul(world2Space, skinTransform);

                var vertex = originalVertices[uIndex];
                var outPosition = math.mul(skinTransform, new float4(vertex.position, 1f));
                var outNormal = math.mul(skinTransform, new float4(vertex.Normal, 0f));
                var outTangent = math.mul(skinTransform, new float4(vertex.Tangent, 0f));

                outVertices[(uint)index] = new CompressedVertex(outPosition, outNormal, outTangent);
            }
        }

        [BurstCompile]
        struct FillMesh : IJobFor {
            [ReadOnly] public UnsafeArray<CompressedVertex> skinnedVertices;
            [ReadOnly] public UnsafeArray<AdditionalVertexData> additionalData;

            public UnsafeArray<FullVertex>.Span outMeshVertices;
            public UnsafeArray<float2>.Span outMeshUVs;

            public void Execute(int index) {
                var uIndex = (uint)index;
                var vertex = skinnedVertices[uIndex];
                var additionalDatum = additionalData[uIndex];
                outMeshVertices[uIndex] = new FullVertex(vertex.position, vertex.Normal, vertex.Tangent, additionalDatum.tangentW);
                outMeshUVs[uIndex] = additionalDatum.UV;
            }
        }

        [BurstCompile]
        struct UpdateMesh : IJobFor {
            [ReadOnly] public UnsafeArray<CompressedVertex> skinnedVertices;
            [ReadOnly] public UnsafeArray<AdditionalVertexData> additionalData;

            public UnsafeArray<FullVertex>.Span outMeshVertices;

            public void Execute(int index) {
                var uIndex = (uint)index;
                var vertex = skinnedVertices[uIndex];
                var additionalDatum = additionalData[uIndex];
                outMeshVertices[uIndex] = new FullVertex(vertex.position, vertex.Normal, vertex.Tangent, additionalDatum.tangentW);
            }
        }

        struct FullVertex {
            public float3 position;
            public float3 normal;
            public float4 tangent;

            public FullVertex(float3 position, float3 normal, float3 tangent, float tangentHandedness) {
                this.position = position;
                this.normal = normal;
                this.tangent = new float4(tangent, tangentHandedness);
            }
        }
    }
}
