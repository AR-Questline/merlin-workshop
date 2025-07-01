using System;

namespace Unity.Rendering
{
    /// <summary>
    /// Marks an IComponentData as an input to a material property on a particular shader.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
    public class MaterialPropertyAttribute : Attribute
    {
        /// <summary>
        /// Constructs a material property attribute.
        /// </summary>
        /// <param name="materialPropertyName">The name of the material property.</param>
        /// <param name="overrideSizeGPU">An optional size of the property on the GPU. This is in bytes.</param>
        public MaterialPropertyAttribute(string materialPropertyName, short overrideSizeGPU = -1)
        {
            Name = materialPropertyName;
            OverrideSizeGPU = overrideSizeGPU;
        }

        /// <summary>
        /// The name of the material property.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The size of the property in bytes on the GPU.
        /// </summary>
        public short OverrideSizeGPU { get; }
    }
}
