//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(DissolveFilter), true)]
	[CanEditMultipleObjects]
	internal class DissolveFilterEditor : FilterBaseEditor
	{
		private static readonly AboutInfo s_aboutInfo = 
				new AboutInfo(s_aboutHelp, "UIFX - Dissolve Filter\n© Chocolate Dinosaur Ltd", "uifx-icon")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/bundle/components/dissolve-filter/"),
							}
						},
						new AboutSection("Unity Asset Store Review\r\n<color=#ffd700>★★★★☆</color>")
						{
							buttons = new AboutButton[]
							{
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

		private static readonly GUIContent Content_Dissolve = new GUIContent("Dissolve");
		private static readonly GUIContent Content_Edge = new GUIContent("Edge");
		private static readonly GUIContent Content_ScaleMode = new GUIContent("Scale Mode");
		private static readonly GUIContent Content_Length = new GUIContent("Length");
		private static readonly GUIContent Content_ColorMode = new GUIContent("Color Mode");
		private static readonly GUIContent Content_Ramp = new GUIContent("Ramp");
		private static readonly GUIContent Content_Emissive = new GUIContent("Emissive");

		private SerializedProperty _propDissolve;
		private SerializedProperty _propTexture;
		private SerializedProperty _propTextureScaleMode;
		private SerializedProperty _propScale;
		private SerializedProperty _propInvert;
		private SerializedProperty _propEdgeLength;
		private SerializedProperty _propEdgeColorMode;
		private SerializedProperty _propEdgeColor;
		private SerializedProperty _propEdgeTexture;
		private SerializedProperty _propEdgeEmissive;
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