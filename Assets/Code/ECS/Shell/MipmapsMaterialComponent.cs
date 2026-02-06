using System;
using Awaken.Utility.Graphics.Mipmaps;
using Unity.Entities;

namespace Awaken.ECS.Mipmaps.Components {
    public struct MipmapsMaterialComponent : IComponentData {
        public MipmapsStreamingMasterMaterials.MaterialId id;

        public MipmapsMaterialComponent(MipmapsStreamingMasterMaterials.MaterialId id) {
            throw new NotImplementedException();
        }
    }
}