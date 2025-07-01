//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEngine.UI;
using UnityEditor; 

namespace ChocDino.UIFX.Editor
{
	internal class FilterBaseEditor : BaseEditor
	{
		protected static readonly string TextMeshProGraphicTypeName = "TMPro.TextMeshProUGUI";
		protected static readonly string FilterStackTextMeshProFullTypeName = "ChocDino.UIFX.FilterStackTextMeshPro, ChocDino.UIFX.TMP";
		protected static readonly string FilterStackTextMeshProComponentName = "FilterStackTextMeshPro";

		protected static readonly GUIContent Content_R = new GUIContent("R");
		protected static readonly GUIContent Content_Space = new GUIContent(" ");
		protected static readonly GUIContent Content_Size = new GUIContent("Size");
		protected static readonly GUIContent Content_Color = new GUIContent("Color");
		protected static readonly GUIContent Content_Colors = new GUIContent("Colors");
		protected static readonly GUIContent Content_Apply = new GUIContent("Apply");
		protected static readonly GUIContent Content_Border = new GUIContent("Border");
		protected static readonly GUIContent Content_Fill = new GUIContent("Fill");
		protected static readonly GUIContent Content_Preview = new GUIContent("Preview");
		protected static readonly GUIContent Content_Stop = new GUIContent("Stop");
		protected static readonly GUIContent Content_Texture = new GUIContent("Texture");
		protected static readonly GUIContent Content_Gradient = new GUIContent("Gradient");
		protected static readonly GUIContent Content_Reverse = new GUIContent("Reverse");

		private static readonly GUIContent Content_Debug = new GUIContent("Debug");
		private static readonly GUIContent Content_Strength = new GUIContent("Strength");
		private static readonly GUIContent Content_SaveToPNG = new GUIContent("Save to PNG", "TextMeshPro is not currently supported.");
		private static readonly GUIContent Content_BakeToImage = new GUIContent("Bake To Image", "Bake all filters above to an Image component. Baking currently doesn't support: world-space canvas, rotation or scale, and unmatching anchor min/max values, TextMeshPro.");
		private static readonly GUIContent Content_PreviewTitle= new GUIContent("UIFX - Filters");

		private void ShowSaveToPngButton()
        {
        }

#if !UIFX_FILTER_HIDE_INSPECTOR_PREVIEW

        private string GetFilterShortName()
        {
            return default;
        }

        public override GUIContent GetPreviewTitle()
        {
            return default;
        }

        public override bool HasPreviewGUI()
        {
            return default;
        }

        public override string GetInfoString()
        {
            return default;
        }

        public override void OnPreviewSettings()
        {
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
        }
#endif

        private static void DrawTexture(Texture texture, bool showAlphaBlended)
        {
        }

        private bool CanBakeFiltersToImage(FilterBase filter)
        {
            return default;
        }

        private bool CanSaveFilterToPng(FilterBase filter)
        {
            return default;
        }

        private bool BakeFiltersToImage(FilterBase filter, bool makeCopy)
        {
            return default;
        }

        private bool ShowBakeFiltersToImageDialog(FilterBase filter)
        {
            return default;
        }

        internal bool OnInspectorGUI_Baking(FilterBase filter)
        {
            return default;
        }

        // Returns true if checks fail and the rest of the inspector shouldn't be shown
        internal bool OnInspectorGUI_Check(FilterBase filter)
        {
            return default;
        }

        internal static void OnInspectorGUI_Debug(FilterBase filter)
        {
        }

        internal static void PropertyReset_Slider(SerializedProperty prop, GUIContent label, float min, float max, float resetValue)
        {
        }

        internal static void PropertyReset_Float(SerializedProperty prop, float resetValue)
        {
        }

        internal static void DrawStrengthProperty(SerializedProperty prop)
        {
        }

        internal static bool DrawStrengthProperty(ref float value)
        {
            return default;
        }

        internal static bool DrawStrengthProperty(Rect rect, ref float value)
        {
            return default;
        }

        internal static void DrawDualColors(SerializedProperty col1, SerializedProperty col2, GUIContent label)
        {
        }

        protected static void SaveTexture(RenderTexture texture)
        {
        }
    }
}