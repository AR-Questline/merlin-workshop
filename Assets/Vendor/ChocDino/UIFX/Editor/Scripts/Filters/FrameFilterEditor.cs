//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(FrameFilter), true)]
	[CanEditMultipleObjects]
	internal class FrameFilterEditor : FilterBaseEditor
	{
		private static readonly AboutInfo s_aboutInfo = 
				new AboutInfo(s_aboutHelp, "UIFX - Frame Filter\n© Chocolate Dinosaur Ltd", "uifx-logo-frame-filter")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("User Guide", "https://www.chocdino.com/products/uifx/frame-filter/about/"),
								new AboutButton("Scripting Guide", "https://www.chocdino.com/products/uifx/frame-filter/scripting/"),
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/frame-filter/components/blur-filter/"),
								new AboutButton("API Reference", "https://www.chocdino.com/products/uifx/frame-filter/API/ChocDino.UIFX/"),
							}
						},
						new AboutSection("Unity Asset Store Review\r\n<color=#ffd700>★★★★☆</color>")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Review <b>UIFX - Frame Filter</b>", "https://assetstore.unity.com/packages/slug/266945?aid=1100lSvNe#reviews"),
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

		private static readonly GUIContent Content_Shape = new GUIContent("Shape");
		private static readonly GUIContent Content_Radius = new GUIContent("Radius");
		private static readonly GUIContent Content_Padding = new GUIContent("Padding");
		private static readonly GUIContent Content_PadToEdge = new GUIContent("Pad To Edge");
		private static readonly GUIContent Content_RoundCorners = new GUIContent("Round Corners");
		private static readonly GUIContent Content_Percent = new GUIContent("Percent");
		private static readonly GUIContent Content_Pixels = new GUIContent("Pixels");
		private static readonly GUIContent Content_Softness = new GUIContent("Softness");
		private static readonly GUIContent Content_GradientShape = new GUIContent("Gradient Shape");
		private static readonly GUIContent Content_FillMode = new GUIContent("Fill Mode");

		private SerializedProperty _propColor;
		private SerializedProperty _propSoftness;
		private SerializedProperty _propFillMode;
		private SerializedProperty _propTexture;
		private SerializedProperty _propGradient;
		private SerializedProperty _propGradientShape;
		private SerializedProperty _propGradientRadialRadius;
		//private SerializedProperty _propTextureScaleMode;
		//private SerializedProperty _propTextureScale;
		//private SerializedProperty _propSprite;
		private SerializedProperty _propShape;
		private SerializedProperty _propRectPadding;
		private SerializedProperty _propRadiusPadding;
		private SerializedProperty _propRectToEdge;
		private SerializedProperty _propRectRoundCornerMode;
		private SerializedProperty _rectRoundCornersValue;
		private SerializedProperty _propRectRoundCorners;
		private SerializedProperty _propCutoutSource;
		private SerializedProperty _propBorderColor;
		private SerializedProperty _propBorderFillMode;
		private SerializedProperty _propBorderTexture;
		private SerializedProperty _propBorderGradient;
		private SerializedProperty _propBorderGradientShape;
		private SerializedProperty _propBorderGradientRadialRadius;
		private SerializedProperty _propBorderSize;
		private SerializedProperty _propBorderSoftness;
		private SerializedProperty _propStrength;
		private SerializedProperty _propRenderSpace;
		private SerializedProperty _propExpand;

		protected virtual void OnEnable()
        {
        }

        private static void Slider(SerializedProperty prop, float min, float max, GUIContent label = null)
        {
        }

        public override void OnInspectorGUI()
        {
        }
    }
}