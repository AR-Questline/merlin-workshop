//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(BlurFilter), true)]
	[CanEditMultipleObjects]
	internal class BlurFilterEditor : FilterBaseEditor
	{
		internal static readonly AboutInfo s_aboutInfo = 
				new AboutInfo(s_aboutHelp, "UIFX - Blur Filter\n© Chocolate Dinosaur Ltd", "uifx-logo-blur-filter")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("User Guide", "https://www.chocdino.com/products/uifx/blur-filter/about/"),
								new AboutButton("Scripting Guide", "https://www.chocdino.com/products/uifx/blur-filter/scripting/"),
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/blur-filter/components/blur-filter/"),
								new AboutButton("API Reference", "https://www.chocdino.com/products/uifx/blur-filter/API/ChocDino.UIFX/"),
							}
						},
						new AboutSection("Unity Asset Store Review\r\n<color=#ffd700>★★★★☆</color>")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Review <b>UIFX - Blur Filter</b>", "https://assetstore.unity.com/packages/slug/268262?aid=1100lSvNe#reviews"),
								new AboutButton("Review <b>UIFX Bundle</b>", AssetStoreBundleReviewUrl),
							}
						},
						new AboutSection("UIFX Support")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Discord Community", DiscordUrl),
								new AboutButton("Post to Unity Discussions", "https://discussions.unity.com/t/released-uifx-blur-filter/936189"),
								new AboutButton("Post Issues to GitHub", GithubUrl),
								new AboutButton("Email Us", SupportEmailUrl),
							}
						}
					}
				};

		private static readonly AboutToolbar s_aboutToolbar = new AboutToolbar(new AboutInfo[] { s_upgradeToBundle, s_aboutInfo } );

		private static readonly GUIContent Content_FadeCurve = new GUIContent("Fade Curve");
		private static readonly GUIContent Content_Axes = new GUIContent("Axes");
		private static readonly GUIContent Content_Blur = new GUIContent("Blur");
		private static readonly GUIContent Content_Global = new GUIContent("Global");

		private SerializedProperty _propBlurAlgorithm;
		private SerializedProperty _propBlurAxes2D;
		private SerializedProperty _propDownsample;
		private SerializedProperty _propBlur;
		private SerializedProperty _propApplyAlphaCurve;
		private SerializedProperty _propAlphaCurve;
		private SerializedProperty _propStrength;
		private SerializedProperty _propRenderSpace;
		private SerializedProperty _propExpand;
		//protected SerializedProperty _propIncludeChildren;

		protected virtual void OnEnable()
        {
        }

        public override void OnInspectorGUI()
        {
        }
    }
}