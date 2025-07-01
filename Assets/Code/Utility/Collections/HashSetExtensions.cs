using System.Collections.Generic;

namespace Awaken.Utility.Collections {
    public static class HashSetExtensions {
        public static bool HasAnyCommonValue<T>(this HashSet<T> left, HashSet<T> right) {
            foreach (var value in left) {
                if (right.Contains(value)) {
                    return true;
                }
            }

            return false;
        } 
    }
}