using System;
using Awaken.Utility.Maths;
using Unity.Mathematics;

namespace Awaken.Kandra.Data {
    [Serializable]
    public struct Vertex {
        public float3 position;
        public float3 normal;
        public float3 tangent;

        public Vertex(float3 position, float3 normal, float3 tangent) {
            this.position = position;
            this.normal = normal;
            this.tangent = tangent;
        }

        public override string ToString() {
            return $"Position: {position}, Normal: {normal}|{math.length(normal)}, Tangent: {tangent}|{math.length(tangent)}";
        }
    }

    [Serializable]
    public struct CompressedVertex : IEquatable<CompressedVertex>, IEquatable<Vertex> {
        public float3 position;
        public uint2 normalAndTangent;

        public float3 Normal => CompressionUtils.DecodeNormalVectorOctahedron(normalAndTangent.x);
        public float3 Tangent => CompressionUtils.DecodeNormalVectorOctahedron(normalAndTangent.y);

        public CompressedVertex(Vertex vertex) : this(vertex.position, vertex.normal, vertex.tangent) { }

        public CompressedVertex(float3 position, float3 normal, float3 tangent) {
            this.position = position;
            this.normalAndTangent = CompressionUtils.EncodeNormalAndTangent(math.normalizesafe(normal), math.normalizesafe(tangent));
        }

        public static implicit operator CompressedVertex(Vertex vertex) => new CompressedVertex(vertex);
        public static implicit operator Vertex(CompressedVertex compressedVertex) => new Vertex(compressedVertex.position, compressedVertex.Normal, compressedVertex.Tangent);

        public bool Equals(Vertex other) {
            return position.Equals(other.position) && normalAndTangent.Equals(CompressionUtils.EncodeNormalAndTangent(Normal, Tangent));
        }

        public bool Equals(CompressedVertex other) {
            return position.Equals(other.position) && normalAndTangent.Equals(other.normalAndTangent);
        }

        public override bool Equals(object obj) {
            return obj is CompressedVertex other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                return (position.GetHashCode() * 397) ^ normalAndTangent.GetHashCode();
            }
        }

        public override string ToString() {
            return $"Position: {position}, Normal: {Normal}|{math.length(Normal)}, Tangent: {Tangent}|{math.length(Tangent)} ({normalAndTangent})";
        }
    }
}