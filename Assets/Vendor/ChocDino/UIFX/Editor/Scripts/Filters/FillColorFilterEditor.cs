//--------------------------------------------------------------------------//
// Copyright 2023 Chocolate Dinosaur Ltd. All rights reserved.              //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(FillColorFilter), true)]
	[CanEditMultipleObjects]
	internal class FillColorFilterEditor : FilterBaseEditor
	{
		private static readonly AboutInfo s_aboutInfo = 
				new AboutInfo(s_aboutHelp, "UIFX - Fill Color Filter\n© Chocolate Dinosaur Ltd", "uifx-logo-fill-filter")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/bundle/components/fill-color-filter/"),
							}
						},
						new AboutSection("Unity Asset Store Review\r\n<color=#ffd700>★★★★☆</color>")
						{
							buttons = new AboutButton[]
							{
								//new AboutButton("Review <b>UIFX - Fill Filter</b>", "https://assetstore.unity.com/packages/slug/274847?aid=1100lSvNe#reviews"),
								new AboutButton("Review <b>UIFX Bundle</b>", "https://assetstore.unity.com/packages/slug/266945?aid=1100lSvNe#reviews"),
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

		private SerializedProperty _propMode;
		private SerializedProperty _propColor;
		private SerializedProperty _propColorA;
		private SerializedProperty _propColorB;
		private SerializedProperty _propColorTL;
		private SerializedProperty _propColorTR;
		private SerializedProperty _propColorBL;
		private SerializedProperty _propColorBR;
		private SerializedProperty _propColorScale;
		private SerializedProperty _propColorBias;
		private SerializedProperty _propComposite;
		private SerializedProperty _propStrength;
		private SerializedProperty _propRenderSpace;

		void OnEnable()
        {
        }

        public override void OnInspectorGUI()
        {
        }
    }
}