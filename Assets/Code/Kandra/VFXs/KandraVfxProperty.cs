using System;
using UnityEngine.VFX;

// VFX graphs are already serialized with old namespace so we need to keep it
// ReSharper disable once CheckNamespace
namespace Awaken.Kandra {
    [VFXType(VFXTypeAttribute.Usage.Default | VFXTypeAttribute.Usage.GraphicsBuffer), Serializable]
    public struct KandraVfxProperty {
        public uint vertexStart;
        public uint additionalDataStart;
        public uint vertexCount;
        public uint trianglesCount;
        public KandraVfxProperty(uint vertexStart, uint additionalDataStart, uint vertexCount, uint trianglesCount) {
            this.vertexStart = vertexStart;
            this.additionalDataStart = additionalDataStart;
            this.vertexCount = vertexCount;
            this.trianglesCount = trianglesCount;
        }
    }
}
