using UnityEngine;

namespace Pathfinding.Util {
	/// <summary>Compatibility class for Unity APIs that are not available in all Unity versions</summary>
	public static class UnityCompatibility {
		public static T[] FindObjectsByTypeSorted<T>() where T : Object {
            return default;
        }

        public static T[] FindObjectsByTypeUnsorted<T>() where T : Object
        {
            return default;
        }

        public static T[] FindObjectsByTypeUnsortedWithInactive<T>() where T : Object
        {
            return default;
        }

        public static T FindAnyObjectByType<T>() where T : Object
        {
            return default;
        }
    }
}

#if !UNITY_2022_3_OR_NEWER
namespace Pathfinding {
	public class IgnoredByDeepProfilerAttribute : System.Attribute {
	}
}
#endif
