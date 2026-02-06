using System;
using Unity.Mathematics;

namespace Awaken.Kandra.Data {
    [Serializable]
    public struct Vertex {
        public float3 position;
        public float3 normal;
        public float3 tangent;

        public Vertex(float3 position, float3 normal, float3 tangent) {
            throw new NotImplementedException();
        }

        public override string ToString() {
            throw new NotImplementedException();
        }
    }

    public struct CompressedVertex : IEquatable<CompressedVertex>, IEquatable<Vertex> {
        public float3 position;
        public uint2 normalAndTangent;

        public float3 Normal;
        public float3 Tangent;

        public CompressedVertex(Vertex vertex) : this(vertex.position, vertex.normal, vertex.tangent) {
        }

        public CompressedVertex(float3 position, float3 normal, float3 tangent) {
            throw new NotImplementedException();
        }

        public static implicit operator CompressedVertex(Vertex vertex) => throw new NotImplementedException();

        public static implicit operator Vertex(CompressedVertex compressedVertex) =>
            throw new NotImplementedException();

        public bool Equals(Vertex other) {
            throw new NotImplementedException();
        }

        public bool Equals(CompressedVertex other) {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj) {
            throw new NotImplementedException();
        }

        public override int GetHashCode() {
            throw new NotImplementedException();
        }

        public override string ToString() {
            throw new NotImplementedException();
        }
    }
}