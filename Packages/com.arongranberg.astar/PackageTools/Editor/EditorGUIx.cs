using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace Pathfinding {
	/// <summary>Simple GUI utility functions</summary>
	public static class GUIUtilityx {
		static Stack<Color> colors = new Stack<Color>();

		public static void PushTint (Color tint) {
        }

        public static void PopTint()
        {
        }

        public static Rect SliceRow(ref Rect rect, float height)
        {
            return default;
        }

        public static Rect SliceColumn(ref Rect rect, float width, float spacing = 0)
        {
            return default;
        }
    }

	/// <summary>
	/// Editor helper for hiding and showing a group of GUI elements.
	/// Call order in OnInspectorGUI should be:
	/// - Begin
	/// - Header/HeaderLabel (optional)
	/// - BeginFade
	/// - [your gui elements] (if BeginFade returns true)
	/// - End
	/// </summary>
	public class FadeArea {
		Rect lastRect;
		float value;
		float lastUpdate;
		GUIStyle labelStyle;
		GUIStyle areaStyle;
		bool visible;
		Editor editor;

		/// <summary>
		/// Is this area open.
		/// This is not the same as if any contents are visible, use <see cref="BeginFade"/> for that.
		/// </summary>
		public bool open;

		/// <summary>Animate dropdowns when they open and close</summary>
		public static bool fancyEffects;
		const float animationSpeed = 100f;

		public FadeArea (bool open, Editor editor, GUIStyle areaStyle, GUIStyle labelStyle = null) {
        }

        void Tick () {
        }

        public void Begin () {
        }

        public void HeaderLabel (string label) {
        }

        public void Header (string label) {
        }

        public void Header(string label, ref bool open)
        {
        }

        /// <summary>Hermite spline interpolation</summary>
        static float Hermite(float start, float end, float value)
        {
            return default;
        }

        public bool BeginFade()
        {
            return default;
        }

        public void End()
        {
        }
    }
	/// <summary>Handles fading effects and also some custom GUI functions such as LayerMaskField</summary>
	public static class EditorGUILayoutx {
		static Dictionary<int, string[]> layerNames = new Dictionary<int, string[]>();
		static long lastUpdateTick;
		static List<string> dummyList = new List<string>();

		/// <summary>Displays a LayerMask field.</summary>
		/// <param name="label">Label to display</param>
		/// <param name="selected">Current LayerMask</param>
		public static LayerMask LayerMaskField (string label, LayerMask selected) {
            return default;
        }
    }
}
