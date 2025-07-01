//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(GooeyFilter), true)]
	[CanEditMultipleObjects]
	internal class GooeyFilterEditor : FilterBaseEditor	
	{

		private static readonly AboutInfo s_aboutInfo = 
				new AboutInfo(s_aboutHelp, "UIFX - Gooey Filter\n© Chocolate Dinosaur Ltd", "uifx-logo-gooey-filter")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("User Guide", "https://www.chocdino.com/products/uifx/gooey-filter/about/"),
								new AboutButton("Scripting Guide", "https://www.chocdino.com/products/uifx/gooey-filter/scripting/"),
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/gooey-filter/components/gooey-filter/"),
								new AboutButton("API Reference", "https://www.chocdino.com/products/uifx/gooey-filter/API/ChocDino.UIFX/"),
							}
						},
						new AboutSection("Unity Asset Store Review\r\n<color=#ffd700>★★★★☆</color>")
						{
							buttons = new AboutButton[]
							{
								//new AboutButton("Review <b>UIFX - Gooey Filter</b>", "https://assetstore.unity.com/packages/slug/266945?aid=1100lSvNe#reviews"),
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

		private static readonly GUIContent Content_Gooey = new GUIContent("Gooey");

		private SerializedProperty _propSize;
		private SerializedProperty _propBlur;
		private SerializedProperty _propThreshold;
		private SerializedProperty _propThresholdFalloff;
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