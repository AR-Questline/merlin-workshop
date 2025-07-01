//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(LongShadowFilter), true)]
	[CanEditMultipleObjects]
	internal class LongShadowFilterEditor : FilterBaseEditor
	{
		private static readonly AboutInfo s_aboutInfo = 
				new AboutInfo(s_aboutHelp, "UIFX - Long Shadow Filter\n© Chocolate Dinosaur Ltd", "uifx-logo-long-shadow-filter")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("User Guide", "https://www.chocdino.com/products/uifx/extrude-filter/about/"),
								new AboutButton("Scripting Guide", "https://www.chocdino.com/products/uifx/extrude-filter/scripting/"),
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/extrude-filter/components/long-shadow-filter/"),
								new AboutButton("API Reference", "https://www.chocdino.com/products/uifx/extrude-filter/API/ChocDino.UIFX/"),
							}
						},
						new AboutSection("Unity Asset Store Review\r\n<color=#ffd700>★★★★☆</color>")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Review <b>UIFX - Extrude Filter</b>", "https://assetstore.unity.com/packages/slug/276742?aid=1100lSvNe#reviews"),
								new AboutButton("Review <b>UIFX Bundle</b>", AssetStoreBundleReviewUrl),
							}
						},
						new AboutSection("UIFX Support")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Discord Community", DiscordUrl),
								new AboutButton("Post to Unity Discussions", "https://discussions.unity.com/t/released-uifx-long-shadow-filter/941048"),
								new AboutButton("Post Issues to GitHub", GithubUrl),
								new AboutButton("Email Us", SupportEmailUrl),
							}
						}
					}
				};

		private static readonly AboutToolbar s_aboutToolbar = new AboutToolbar(new AboutInfo[] { s_upgradeToBundle, s_aboutInfo } );

		private static readonly GUIContent Content_LongShadow = new GUIContent("Long Shadow");
		private static readonly GUIContent Content_Distance = new GUIContent("Distance");

		private SerializedProperty _propMethod;
		private SerializedProperty _propAngle;
		private SerializedProperty _propDistance;
		private SerializedProperty _propStepSize;
		private SerializedProperty _propColor1;
		private SerializedProperty _propUseBackColor;
		private SerializedProperty _propColor2;
		private SerializedProperty _propPivot;
		private SerializedProperty _propSourceAlpha;
		private SerializedProperty _propCompositeMode;
		private SerializedProperty _propStrength;
		private SerializedProperty _propRenderSpace;
		private SerializedProperty _propExpand;

		void OnEnable()
        {
        }

        public override void OnInspectorGUI()
        {
        }
    }
}