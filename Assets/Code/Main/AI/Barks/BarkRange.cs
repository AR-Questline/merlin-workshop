using System;

namespace Awaken.TG.Main.AI.Barks {
    public enum BarkRange {
        Short,
        Medium,
        Long,
        Unlimited
    }

    public static class BarkRangeExtensions {
        public const float RangeShort = 20f;
        public const float RangeMedium = 40f;
        public const float RangeLong = 70f;
        public const float RangeUnlimited = 1000f;
        
        public static float ToFloat(this BarkRange range) =>
            range switch {
                BarkRange.Short => RangeShort,
                BarkRange.Medium => RangeMedium,
                BarkRange.Long => RangeLong,
                BarkRange.Unlimited => RangeUnlimited,
                _ => throw new ArgumentOutOfRangeException(nameof(range), range, null)
            };
    }
}