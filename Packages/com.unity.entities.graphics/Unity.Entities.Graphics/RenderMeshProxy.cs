using System;
using System.Collections.Generic;
using Unity.Assertions;
using Unity.Core;
using Unity.Entities;
using UnityEngine;

namespace Unity.Rendering
{
    /// <summary>
    /// Defines the mesh and rendering properties of an entity during baking.
    /// </summary>
    [Serializable]
    // Culling system requires a maximum of 128 entities per chunk (See ChunkInstanceLodEnabled)
    [MaximumChunkCapacity(128)]
    [TemporaryBakingType]
    public struct RenderMesh : ISharedComponentData, IEquatable<RenderMesh>
    {
        /// <summary>
        /// A reference to a [UnityEngine.Mesh](https://docs.unity3d.com/ScriptReference/Mesh.html) object.
        /// </summary>
        public Mesh                 mesh;

        /// <summary>
        /// The material list.
        /// </summary>
        public List<Material>       materials;

        /// <summary>
        /// The submesh index.
        /// </summary>
        public int                  subMesh;

        /// <summary>
        /// A reference to a [UnityEngine.Material](https://docs.unity3d.com/ScriptReference/Material.html) object.
        /// </summary>
        /// <remarks>For efficient rendering, the material should enable GPU instancing.
        /// For entities converted from GameObjects, this value is derived from the Materials array of the source
        /// Mesh Renderer Component.
        /// </remarks>
        public Material             material
        {
            get
            {
                if (materials == null || subMesh >= materials.Count)
                    return null;

                return materials[subMesh];
            }

            set
            {
                if (materials == null || subMesh >= materials.Count)
                    return;

                materials[subMesh] = value;
            }
        }

        /// <summary>
        /// Constructs a RenderMesh using the given Renderer, Mesh, optional list of shared Materials, and option sub-mesh index.
        /// </summary>
        /// <param name="renderer">The Renderer to use.</param>
        /// <param name="mesh">The Mesh to use.</param>
        /// <param name="sharedMaterials">An optional list of Materials to use.</param>
        /// <param name="subMeshIndex">An options sub-mesh index that represents a sub-mesh in the mesh parameter.</param>
        public RenderMesh(
            Renderer renderer,
            Mesh mesh,
            List<Material> sharedMaterials = null,
            int subMeshIndex = 0)
        {
            Assert.IsTrue(renderer != null, "Must have a non-null Renderer to create RenderMesh.");
            Assert.IsTrue(mesh != null, "Must have a non-null Mesh to create RenderMesh.");

            if (sharedMaterials is null)
                sharedMaterials = new List<Material>(capacity: 10);

            if (sharedMaterials.Count == 0)
                renderer.GetSharedMaterials(sharedMaterials);

            Assert.IsTrue(subMeshIndex >= 0 && subMeshIndex < sharedMaterials.Count,
                "Sub-mesh index out of bounds, no matching material.");

            this.mesh = mesh;
            materials = sharedMaterials;
            subMesh = subMeshIndex;
        }

        /// <summary>
        /// Two RenderMesh objects are equal if their respective property values are equal.
        /// </summary>
        /// <param name="other">Another RenderMesh.</param>
        /// <returns>True, if the properties of both RenderMeshes are equal.</returns>
        public bool Equals(RenderMesh other)
        {
            return
                mesh == other.mesh &&
                material == other.material &&
                subMesh == other.subMesh;
        }

        /// <summary>
        /// A representative hash code.
        /// </summary>
        /// <returns>A number that is guaranteed to be the same when generated from two objects that are the same.</returns>
        public override int GetHashCode()
        {
            int hash = 0;

            unsafe
            {
                var buffer = stackalloc[]
                {
                    ReferenceEquals(mesh, null) ? 0 : mesh.GetHashCode(),
                    ReferenceEquals(material, null) ? 0 : material.GetHashCode(),
                    subMesh.GetHashCode(),
                };

                hash = (int)XXHash.Hash32((byte*)buffer, 3 * 4);
            }

            return hash;
        }
    }
}
