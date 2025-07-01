using Unity.Mathematics;

namespace Awaken.TG.LeshyRenderer {
    public struct CatalogCellData {
        public AABB bounds;
        public ushort prefabId;
        public uint instancesCount;
        public uint matricesOffset;
    }
}
