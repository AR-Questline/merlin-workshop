using UnityEditor;
using UnityEngine;

namespace Pathfinding {
	public class GraphEditor : GraphEditorBase {
		public AstarPathEditor editor;

		/// <summary>Stores if the graph is visible or not in the inspector</summary>
		public FadeArea fadeArea;

		/// <summary>Stores if the graph info box is visible or not in the inspector</summary>
		public FadeArea infoFadeArea;

		public virtual void OnEnable () {
        }

        /// <summary>Rounds a vector's components to multiples of 0.5 (i.e 0.5, 1.0, 1.5, etc.) if very close to them</summary>
        public static Vector3 RoundVector3 (Vector3 v) {
            return default;
        }

        public static Object ObjectField (string label, Object obj, System.Type objType, bool allowSceneObjects, bool assetsMustBeInResourcesFolder) {
            return default;
        }

        public static Object ObjectField (GUIContent label, Object obj, System.Type objType, bool allowSceneObjects, bool assetsMustBeInResourcesFolder) {
            return default;
        }

        /// <summary>Draws common graph settings</summary>
        public void OnBaseInspectorGUI (NavGraph target) {
        }

        /// <summary>Override to implement graph inspectors</summary>
        public virtual void OnInspectorGUI (NavGraph target) {
        }

        /// <summary>Override to implement scene GUI drawing for the graph</summary>
        public virtual void OnSceneGUI(NavGraph target)
        {
        }

        public static void Header(string title)
        {
        }

        /// <summary>Draws a thin separator line</summary>
        public static void Separator()
        {
        }

        /// <summary>Draws a small help box with a 'Fix' button to the right. Returns: Boolean - Returns true if the button was clicked</summary>
        public static bool FixLabel(string label, string buttonLabel = "Fix", int buttonWidth = 40)
        {
            return default;
        }

        /// <summary>Draws a toggle with a bold label to the right. Does not enable or disable GUI</summary>
        public bool ToggleGroup(string label, bool value)
        {
            return default;
        }

        /// <summary>Draws a toggle with a bold label to the right. Does not enable or disable GUI</summary>
        public static bool ToggleGroup(GUIContent label, bool value)
        {
            return default;
        }
    }
}
