//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	public class BaseEditor : UnityEditor.Editor
	{
		protected const string s_aboutHelp = "About & Help";

		protected static readonly string DiscordUrl = "https://discord.gg/wKRzKAHVUE";
		protected static readonly string ForumBundleUrl = "https://discussions.unity.com/t/released-uifx-bundle-advanced-effects-for-unity-ui/940575";
		protected static readonly string GithubUrl = "https://github.com/Chocolate-Dinosaur/UIFX/issues";
		protected static readonly string SupportEmailUrl = "mailto:support@chocdino.com";
		protected static readonly string UIFXBundleWebsiteUrl = "https://www.chocdino.com/products/uifx/bundle/about/";
		protected static readonly string AssetStoreBundleUrl = "https://assetstore.unity.com/packages/slug/266945?aid=1100lSvNe";
		protected static readonly string AssetStoreBundleReviewUrl = "https://assetstore.unity.com/packages/slug/266945?aid=1100lSvNe#reviews";

		internal const string PrefKey_BakedImageSubfolder = "UIFX.BakedImageSubfolder";
		internal const string DefaultBakedImageAssetsSubfolder = "Baked-Images";
		
		internal static readonly AboutInfo s_upgradeToBundle = 
				new AboutInfo("Upgrade ★", "This asset is part of the <b>UIFX Bundle</b> asset.\r\n\r\nAs an existing customer you are entitled to a discounted upgrade!", "uifx-logo-bundle", BaseEditor.ShowUpgradeBundleButton)
				{
					sections = new AboutSection[]
					{
						new AboutSection("Upgrade")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("<color=#ffd700>★ </color>Upgrade to UIFX Bundle<color=#ffd700> ★</color>", AssetStoreBundleUrl),
							}
						},
						new AboutSection("Read More")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("About UIFX Bundle", UIFXBundleWebsiteUrl),
							}
						},
					}
				};

		internal static bool ShowUpgradeBundleButton(bool dummy)
        {
            return default;
        }

        internal static bool DetectUIFXBundle()
        {
            return default;
        }

        // <summary>
        // Creates a button that toggles between two texts but maintains the same size by using the size of the largest.
        // This is useful so that the button doesn't change size resulting in the mouse cursor no longer being over the button.
        // </summary>
        protected bool ToggleButton(bool value, GUIContent labelTrue, GUIContent labelFalse)
        {
            return default;
        }

        protected void EnumAsToolbarCompact(SerializedProperty prop, GUIContent displayName = null)
        {
        }

        protected void EnumAsToolbar(SerializedProperty prop, GUIContent displayName = null)
        {
        }

        protected void TextureScaleOffset(SerializedProperty propTexture, SerializedProperty propScale, SerializedProperty propOffset, GUIContent displayName)
        {
        }

        protected SerializedProperty VerifyFindProperty(string propertyPath)
        {
            return default;
        }

        internal static SerializedProperty VerifyFindPropertyRelative(SerializedProperty property, string relativePropertyPath)
        {
            return default;
        }

#if false
		void ShowAlignmentSelector()
		{
			GUILayoutOption layout = GUILayout.ExpandWidth(false);
			GUIStyle style = UnityEditor.EditorStyles.toolbarButton;
			//style.margin = new RectOffset(0, 0, 0, 0);
			//style.border = new RectOffset(0, 0, 0, 0);
		
			bool toggle;
			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal();
			toggle = _propGradientCenterX.floatValue == -1f;
			toggle = GUILayout.Toggle(toggle, "┏", style, layout);
			GUILayout.Toggle(false, "┳", style, layout);
			GUILayout.Toggle(false, "┓", style, layout);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Toggle(false, "┣", style, layout);
			GUILayout.Toggle(false, "╋", style, layout);
			GUILayout.Toggle(false, "┫", style, layout);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Toggle(false, "┗", style, layout);
			GUILayout.Toggle(false, "┻", style, layout);
			GUILayout.Toggle(false, "┛", style, layout);
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}
#endif

        internal static Gradient GetGradient(SerializedProperty gradientProperty)
        {
            return default;
        }

        internal static void SetGradient(SerializedProperty gradientProperty, Gradient value)
        {
        }
    }
}