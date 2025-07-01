using System;
using System.Collections.Generic;

namespace UnityEngine.VFX
{
    /// <summary>
    /// Sampling information structure
    /// - position: local position on mesh surface
    /// - uv: uv coordinates on position
    /// </summary>
    [VFXType(VFXTypeAttribute.Usage.GraphicsBuffer), Serializable]
    public struct SampledPositionAndUV {
        [UnityEngine.Scripting.Preserve] public Vector3 position;
        [UnityEngine.Scripting.Preserve] public Vector2 uv;
        
        public SampledPositionAndUV(Vector3 position, Vector2 uv) {
            this.position = position;
            this.uv = uv;
        }
    }

    //Obsolete. Used to preserve old serialized data
    [Serializable]
    public struct TriangleSampling {
        [UnityEngine.Scripting.Preserve] public Vector2 coord;
        [UnityEngine.Scripting.Preserve] public uint index;
    }

    /// <summary>
    /// Cache of mesh data
    /// Contains raw attributes extracted from a readable Mesh
    /// </summary>
    public class MeshData {
        public struct Vertex {
            public Vector3 position;
            public Color color;
            public Vector3 normal;
            public Vector4 tangent;
            public Vector4[] uvs;

            public static Vertex operator +(Vertex a, Vertex b) {
                if (a.uvs.Length != b.uvs.Length)
                    throw new InvalidOperationException("Adding compatible vertex");

                var r = new Vertex() {
                    position = a.position + b.position,
                    color = a.color + b.color,
                    normal = a.normal + b.normal,
                    tangent = a.tangent + b.tangent,
                    uvs = new Vector4[a.uvs.Length]
                };

                for (int i = 0; i < a.uvs.Length; ++i)
                    r.uvs[i] = a.uvs[i] + b.uvs[i];

                return r;
            }

            public static Vertex operator *(float a, Vertex b) {
                var r = new Vertex() {
                    position = a * b.position,
                    color = a * b.color,
                    normal = a * b.normal,
                    tangent = a * b.tangent,
                    uvs = new Vector4[b.uvs.Length]
                };

                for (int i = 0; i < b.uvs.Length; ++i)
                    r.uvs[i] = a * b.uvs[i];

                return r;
            }
        };

        public struct Triangle {
            [UnityEngine.Scripting.Preserve] public uint a, b, c;
        };

        [UnityEngine.Scripting.Preserve] public Vertex[] vertices;
        [UnityEngine.Scripting.Preserve] public Triangle[] triangles;
        [UnityEngine.Scripting.Preserve] public double[] accumulatedTriangleArea;
    }
}