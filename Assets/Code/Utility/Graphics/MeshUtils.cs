using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.Utility.Graphics {
    public static class MeshUtils {
        public static Mesh CopyMesh(Mesh original, Mesh copy = null, bool readable = false) {
            bool hadData = false;
            if (copy == null) {
                copy = new();
            } else {
                hadData = true;
            }
            copy.bounds = original.bounds;

            CopyBaseMeshData(original, copy);
            CopySkinningData(original, copy);

            copy.UploadMeshData(!readable);
            if (hadData) {
                copy.MarkModified();
            }

            return copy;
        }

        static void CopyBaseMeshData(Mesh original, Mesh copy) {
            using var readArray = Mesh.AcquireReadOnlyMeshData(original);
            var readData = readArray[0];

            var vertexFormat = original.GetVertexAttributes();
            var indexFormat = original.indexFormat;
            var isIndexShort = indexFormat == IndexFormat.UInt16;

            var vertexCount = readData.vertexCount;
            var indexCount = isIndexShort ?
                readData.GetIndexData<ushort>().Length :
                readData.GetIndexData<uint>().Length;

            var writeArray = Mesh.AllocateWritableMeshData(1);
            var writeData = writeArray[0];
            writeData.SetVertexBufferParams(vertexCount, vertexFormat);
            writeData.SetIndexBufferParams(indexCount, indexFormat);

            Span<int> streams = stackalloc int[vertexFormat.Length];

            for (var i = 0; i < vertexFormat.Length; i++) {
                streams[vertexFormat[i].stream] = 1;
            }

            for (var i = 0; i < streams.Length; i++) {
                if (streams[i] != 1) {
                    continue;
                }
                var inVert = readData.GetVertexData<byte>(i);
                var outVert = writeData.GetVertexData<byte>(i);
                inVert.CopyTo(outVert);
            }

            var inIndex = readData.GetIndexData<byte>();
            var outIndex = writeData.GetIndexData<byte>();
            inIndex.CopyTo(outIndex);

            writeData.subMeshCount = original.subMeshCount;

            for (var i = 0; i < original.subMeshCount; i++) {
                writeData.SetSubMesh(i, new((int)original.GetIndexStart(i), (int)original.GetIndexCount(i)));
            }

            Mesh.ApplyAndDisposeWritableMeshData(writeArray, copy);
        }

        static void CopySkinningData(Mesh original, Mesh copy) {
            // I don't know about fast and/or low/non alloc API for bindposes and blendShapes
            // But we should tracking Mesh API and replace these heavy calls with light ones whenever possible
            copy.bindposes = original.bindposes;

            var deltaVertices = new Vector3[original.vertexCount];
            var deltaNormals = new Vector3[original.vertexCount];
            var deltaTangents = new Vector3[original.vertexCount];

            var blendShapesCount = original.blendShapeCount;
            for (var shapeIndex = 0; shapeIndex < blendShapesCount; shapeIndex++) {
                var frameCount = original.GetBlendShapeFrameCount(shapeIndex);
                var blendShapeName = original.GetBlendShapeName(shapeIndex);

                for (var frameIndex = 0; frameIndex < frameCount; frameIndex++) {
                    var frameWeight = original.GetBlendShapeFrameWeight(shapeIndex, frameIndex);
                    original.GetBlendShapeFrameVertices(shapeIndex, frameIndex, deltaVertices, deltaNormals,
                        deltaTangents);
                    copy.AddBlendShapeFrame(blendShapeName, frameWeight, deltaVertices, deltaNormals, deltaTangents);
                }
            }
        }
    }
}
