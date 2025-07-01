using System.Runtime.CompilerServices;
using Unity.Burst;

namespace Awaken.TG.Graphics.Culling {
    [BurstCompile]
    public struct DistanceCullerData {
        public const byte Nothing = 0;
        public const int NothingInt = Nothing;
        public const byte IsVisibleMask = 1 << 0;
        public const int IsVisibleMaskInt = IsVisibleMask;
        public const byte HasChangeMask = 1 << 1;
        
        public byte data;

        [MethodImpl(MethodImplOptions.AggressiveInlining), BurstCompile]
        public bool IsVisible() {
            return (data & IsVisibleMask) == IsVisibleMask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), BurstCompile]
        public bool HasChange() {
            return (data & HasChangeMask) == HasChangeMask;
        }
    }
}
