using Unity.Mathematics;

namespace Unity.Rendering
{
    internal struct Fixed16CamDistance
    {
        public const float kRes = 100.0f;

        public static ushort FromFloatCeil(float f)
        {
            return (ushort)math.clamp((int)math.ceil(f * kRes), 0, 0xffff);
        }

        public static ushort FromFloatFloor(float f)
        {
            return (ushort)math.clamp((int)math.floor(f * kRes), 0, 0xffff);
        }
    }

    /// <summary>
    /// Tag to enable instance LOD
    /// </summary>
    internal unsafe struct ChunkInstanceLodEnabled
    {
        public fixed ulong Enabled[2];
    }
}
