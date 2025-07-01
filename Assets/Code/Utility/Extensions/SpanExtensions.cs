using System;

namespace Awaken.Utility.Extensions {
    public static class SpanExtensions {
        public static bool Contains<T>(this Span<T> span, T value) where T : IEquatable<T> {
            for (int i = 0; i < span.Length; i++) {
                if (span[i].Equals(value)) {
                    return true;
                }
            }
            return false;
        }
    }
}