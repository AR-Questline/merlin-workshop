using Sirenix.OdinInspector;
using Unity.Mathematics;

namespace Awaken.TG.Graphics.VFX {
    [System.Serializable]
    public struct DepthTextureStreamingParams {
        public static DepthTextureStreamingParams Default => new() {
            pixelsPerUnit = 16,
            chunkTextureSizeInUnits = 128,
            smoothingAreaRadiusInUnits = 1,
            heightDiffThreshold = 0.2f,
            maxHeightDiff = 5f,
            textureDataMaxBytesToCopyPerFrame = 262144 // 0.25 MB
        };

        public int pixelsPerUnit;
        public int chunkTextureSizeInUnits;
        public float smoothingAreaRadiusInUnits;
        public float heightDiffThreshold;
        public float maxHeightDiff;
        public int textureDataMaxBytesToCopyPerFrame;

        [ReadOnly, ShowInInspector] public int TextureSize => chunkTextureSizeInUnits * pixelsPerUnit;
        [ReadOnly, ShowInInspector] public int SmoothingAreaRadiusInPixels => (int)math.ceil(smoothingAreaRadiusInUnits * pixelsPerUnit);
        
        [ReadOnly, ShowInInspector] string TextureSizeOnDisk => Utility.M.HumanReadableBytes(GetTextureSizeInBytes(TextureSize));

        [ReadOnly, ShowInInspector] string TextureDataMaxBytesToCopyPerFrame => Utility.M.HumanReadableBytes(textureDataMaxBytesToCopyPerFrame);
        
        public static uint GetTextureSizeInBytes(int textureSize) => (uint)(math.square(textureSize) * sizeof(float));
    }
}