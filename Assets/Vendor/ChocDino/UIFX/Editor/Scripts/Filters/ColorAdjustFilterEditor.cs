//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(ColorAdjustFilter), true)]
	[CanEditMultipleObjects]
	internal class ColorAdjustFilterEditor : FilterBaseEditor
	{
		private static readonly AboutInfo s_aboutInfo = 
				new AboutInfo(s_aboutHelp, "UIFX - Color Adjust Filter\n© Chocolate Dinosaur Ltd", "uifx-icon")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/bundle/components/color-adjust-filter/"),
							}
						},
						new AboutSection("Unity Asset Store Review\r\n<color=#ffd700>★★★★☆</color>")
						{
							buttons = new AboutButton[]
							{
								//new AboutButton("Review <b>UIFX - Color Adjust Filter</b>", "https://assetstore.unity.com/packages/slug/266945?aid=1100lSvNe#reviews"),
								new AboutButton("Review <b>UIFX Bundle</b>", AssetStoreBundleReviewUrl),
							}
						},
						new AboutSection("UIFX Support")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Discord Community", DiscordUrl),
								new AboutButton("Post to Unity Discussions", ForumBundleUrl),
								new AboutButton("Post Issues to GitHub", GithubUrl),
								new AboutButton("Email Us", SupportEmailUrl),
							}
						}
					}
				};

		private static readonly AboutToolbar s_aboutToolbar = new AboutToolbar(new AboutInfo[] { s_upgradeToBundle, s_aboutInfo } );

		private static readonly GUIContent Content_ShowAdvancedOptions = new GUIContent("Show Advanced Options");
		private static readonly GUIContent Content_Adjust = new GUIContent("Adjust");
		private static readonly GUIContent Content_Advanced = new GUIContent("Advanced");
		private static readonly GUIContent Content_Brightness = new GUIContent("Brightness");
		private static readonly GUIContent Content_Contrast = new GUIContent("Contrast");
		private static readonly GUIContent Content_Posterize = new GUIContent("Posterize");
		private static readonly GUIContent Content_Red = new GUIContent("Red");
		private static readonly GUIContent Content_Green = new GUIContent("Green");
		private static readonly GUIContent Content_Blue = new GUIContent("Blue");
		private static readonly GUIContent Content_Alpha = new GUIContent("Alpha");

		private const string PrefKey_ShowAdvancedOptions = "ChocDino.ColorAdjustFilter.ShowAdvancedOptions";

		private SerializedProperty _propHue;
		private SerializedProperty _propSaturation;
		private SerializedProperty _propValue;
		private SerializedProperty _propBrightness;
		private SerializedProperty _propContrast;
		private SerializedProperty _propPosterize;
		private SerializedProperty _propOpacity;
		private SerializedProperty[] _propBrightnessRGBA;
		private SerializedProperty[] _propContrastRGBA;
		private SerializedProperty[] _propPosterizeRGBA;
		private SerializedProperty _propRenderSpace;
		private SerializedProperty _propStrength;

		private bool _showAdvancedOptions = false;

		protected SerializedProperty[] VerifyFindVector4Property(string fieldName)
        {
            return default;
        }

        protected static void PropertyReset_Vector4AsRGBA(SerializedProperty[] prop, GUIContent label, float min, float max, float resetValue)
        {
        }

        void OnEnable()
        {
        }

        void OnDisable()
        {
        }

        public override void OnInspectorGUI()
        {
        }
    }
}