using System;
using Awaken.Kandra.Data;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace Awaken.Kandra {
    public static class KandraRendererPoseBaking {
        public static (UnsafeArray<CompressedVertex>, UnsafeArray<AdditionalVertexData>) BakePoseVertices(
            this KandraRenderer kandraRenderer, Allocator allocator) {
            throw new NotImplementedException();
        }

        public static (UnsafeArray<CompressedVertex>, UnsafeArray<AdditionalVertexData>) BakePoseVertices(
            this KandraRenderer kandraRenderer, Allocator allocator, float3x4 world2DesiredSpace) {
            throw new NotImplementedException();
        }

        public static (JobHandle, TransformAccessArray) BakePoseVertices(this KandraRenderer kandraRenderer,
            Allocator allocator, float3x4 world2DesiredSpace, JobHandle dependencies,
            out UnsafeArray<CompressedVertex> skinnedVertices, out UnsafeArray<AdditionalVertexData> additionalData) {
            throw new NotImplementedException();
        }

        public static Mesh BakePoseMesh(this KandraRenderer kandraRenderer) {
            throw new NotImplementedException();
        }

        public static Mesh BakePoseMesh(this KandraRenderer kandraRenderer, float3x4 world2DesiredSpace) {
            throw new NotImplementedException();
        }

        public static void BakePoseMesh(this KandraRenderer kandraRenderer, Mesh mesh, float3x4 world2DesiredSpace) {
            throw new NotImplementedException();
        }

        public static void BakePoseMesh(this KandraRenderer kandraRenderer, Mesh.MeshData meshData,
            float3x4 world2DesiredSpace) {
            throw new NotImplementedException();
        }

        public static void UpdatePoseMesh(this KandraRenderer kandraRenderer, Mesh mesh, float3x4 world2DesiredSpace) {
            throw new NotImplementedException();
        }

        public static void UpdatePoseMesh(this KandraRenderer kandraRenderer, Mesh.MeshData meshData,
            float3x4 world2DesiredSpace) {
            throw new NotImplementedException();
        }

        public static (JobHandle, TransformAccessArray) UpdatePoseMesh(this KandraRenderer kandraRenderer,
            Mesh.MeshData meshData, float3x4 world2DesiredSpace, JobHandle dependencies) {
            throw new NotImplementedException();
        }

        public static Mesh BlankMesh(this KandraRenderer kandraRenderer) {
            throw new NotImplementedException();
        }
    }
}