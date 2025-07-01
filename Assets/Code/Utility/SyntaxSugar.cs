using System.Runtime.CompilerServices;

namespace Awaken.Utility {
    public static class SyntaxSugar {
        /// <summary> Set object value to default and return the previous value </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Nullify<T>(ref T o) {
            var temp = o;
            o = default;
            return temp;
        }
    }
}
