//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(ExtrudeFilter), true)]
	[CanEditMultipleObjects]
	internal class ExtrudeFilterEditor : FilterBaseEditor
	{
		private static readonly AboutInfo s_aboutInfo = 
				new AboutInfo(s_aboutHelp, "UIFX - Extrude Filter\n© Chocolate Dinosaur Ltd", "uifx-logo-extrude-filter")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("User Guide", "https://www.chocdino.com/products/uifx/extrude-filter/about/"),
								new AboutButton("Scripting Guide", "https://www.chocdino.com/products/uifx/extrude-filter/scripting/"),
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/extrude-filter/components/extrude-filter/"),
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
								new AboutButton("Post to Unity Discussions", ForumBundleUrl),
								new AboutButton("Post Issues to GitHub", GithubUrl),
								new AboutButton("Email Us", SupportEmailUrl),
							}
						}
					}
				};

		private static readonly AboutToolbar s_aboutToolbar = new AboutToolbar(new AboutInfo[] { s_upgradeToBundle, s_aboutInfo } );

		private static readonly GUIContent Content_Extrude = new GUIContent("Extrude");
		private static readonly GUIContent Content_Mode = new GUIContent("Mode");
		private static readonly GUIContent Content_BlendMode = new GUIContent("Blend Mode");
		
		private SerializedProperty _propProjection;
		private SerializedProperty _propAngle;
		private SerializedProperty _propDistance;
		private SerializedProperty _propPerspectiveDistance;
		private SerializedProperty _propFillMode;
		private SerializedProperty _propColor1;
		private SerializedProperty _propColor2;
		private SerializedProperty _propGradient;
		private SerializedProperty _propGradientTexture;
		private SerializedProperty _propReverseFill;
		private SerializedProperty _propScrollSpeed;
		private SerializedProperty _propFillBlendMode;
		private SerializedProperty _propSourceAlpha;
		private SerializedProperty _propCompositeMode;
		private SerializedProperty _propStrength;
		private SerializedProperty _propRenderSpace;
		private SerializedProperty _propExpand;

		public override bool RequiresConstantRepaint()
        {
            return default;
        }

        void OnDisable()
        {
        }

        void OnEnable()
        {
        }

        public override void OnInspectorGUI()
        {
        }

        public void OnSceneGUI()
        {
        }
    }
}