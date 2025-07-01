using System.Collections.Generic;
using UnityEngine;

namespace Awaken.TG.Graphics
{
    /// <summary>
    /// Lets us generate meshes dynamically and dispose them in a controlled manner
    /// while avoiding "disposing" of shared meshes taken from assets.
    /// </summary>
    public class MeshGeneration
    {
        public static Dictionary<Mesh, int> s_usageCount = new Dictionary<Mesh, int>();

        public static Mesh GenerateNew(string name) {
            Mesh m = new Mesh();
            s_usageCount[m] = 0;
            return m;
        }

        [UnityEngine.Scripting.Preserve]
        public static void Hold(Mesh mesh) {
            s_usageCount[mesh]++;
        }

        [UnityEngine.Scripting.Preserve]
        public static void Release(Mesh mesh) {
            // is it a generated mesh?
            if (!s_usageCount.ContainsKey(mesh)) return;
            // it is, drop the usage count
            int usesLeft = --s_usageCount[mesh];
            // dispose if nobody is using it anymore
            if (usesLeft == 0) {
                Object.DestroyImmediate(mesh);
                s_usageCount.Remove(mesh);
            }
        }

        [UnityEngine.Scripting.Preserve]
        public static bool IsGenerated(Mesh mesh) => s_usageCount.ContainsKey(mesh);

        // === Generating specific meshes

        /// <summary>
        /// Generates a 3D ring mesh, with UVs going around (U from inner to outer edge, V around counterclockwise).
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public static Mesh GenerateRingMesh(float innerRadius, float outerRadius, int steps) {
            Mesh mesh = GenerateNew($"Ring[{innerRadius}/{outerRadius}]");
            int vertexCount = steps * 2;
            int indexCount = steps * 6;

            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            int[] indices = new int[indexCount];

            float angleStep = Mathf.PI * 2 / steps;
            float vStep = 1f / steps;
            float angle = 0, v = 0;
            for (int i = 0; i < vertexCount; i += 2) {
                float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                vertices[i] = new Vector3(cos * innerRadius, 0, sin * innerRadius);
                vertices[i+1] = new Vector3(cos * outerRadius, 0, sin * outerRadius);
                uvs[i] = new Vector2(0, v);
                uvs[i+1] = new Vector2(1, v);
                
                angle += angleStep;
                v += vStep;
            }
            for (int vi = 0, ti = 0; vi < vertexCount; vi += 2, ti += 6) {
                indices[ti] = vi;
                indices[ti + 1] = (vi + 3) % vertexCount;
                indices[ti + 2] = (vi + 1) % vertexCount;
                indices[ti + 3] = vi;
                indices[ti + 4] = (vi + 2) % vertexCount;
                indices[ti + 5] = (vi + 3) % vertexCount;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);

            return mesh;
        }
    }
}
