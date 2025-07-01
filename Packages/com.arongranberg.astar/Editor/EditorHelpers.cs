using UnityEngine;
using UnityEditor;

namespace Pathfinding.Util {
	/// <summary>Some editor gui helper methods</summary>
	public static class EditorGUILayoutHelper {
		/// <summary>
		/// Tag names and an additional 'Edit Tags...' entry.
		/// Used for SingleTagField
		/// </summary>
		static GUIContent[] tagNamesAndEditTagsButton;
		static int[] tagValues = new [] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, -1 };

		/// <summary>
		/// Last time tagNamesAndEditTagsButton was updated.
		/// Uses EditorApplication.timeSinceStartup
		/// </summary>
		static double timeLastUpdatedTagNames;

		static void FindTagNames () {
        }

        public static int TagField (int value, System.Action editCallback) {
            return default;
        }

        public static int TagField(Rect rect, GUIContent label, int value, System.Action editCallback)
        {
            return default;
        }

        public static int TagField(GUIContent label, int value, System.Action editCallback)
        {
            return default;
        }

        public static void TagField(Rect position, GUIContent label, SerializedProperty property, System.Action editCallback)
        {
        }
    }
}
