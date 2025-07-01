using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Rendering
{
    /// <summary>
    /// An unmanaged component that acts as an example material override for setting the RGBA color of an entity.
    /// </summary>
    /// <remarks>
    /// You should implement your own material property override components inside your project.
    /// </remarks>
    [Serializable]
    [MaterialProperty("_Color")]
    public struct MaterialColor : IComponentData
    {
        /// <summary>
        /// The RGBA color value.
        /// </summary>
        public float4 Value;
    }

    namespace Authoring
    {
        /// <summary>
        /// Represents the authoring component for the material color override.
        /// </summary>
        [DisallowMultipleComponent]
        public class MaterialColor : MonoBehaviour
        {
            /// <summary>
            /// The material color to use.
            /// </summary>
            public Color color;
        }

        /// <summary>
        /// Represents a baker that adds a MaterialColor component to entities this baker affects.
        /// </summary>
        public class MaterialColorBaker : Baker<MaterialColor>
        {
            /// <summary>
            /// Called during the baking process to bake the authoring component.
            /// </summary>
            /// <param name="authoring">The authoring component to bake.</param>
            public override void Bake(MaterialColor authoring)
            {
                Color linearCol = authoring.color.linear;
                var data = new Unity.Rendering.MaterialColor { Value = new float4(linearCol.r, linearCol.g, linearCol.b, linearCol.a) };
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, data);
            }
        }
    }
}
