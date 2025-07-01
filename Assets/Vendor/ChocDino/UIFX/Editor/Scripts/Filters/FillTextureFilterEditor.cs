//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(FillTextureFilter), true)]
	[CanEditMultipleObjects]
	internal class FillTextureFilterEditor : FilterBaseEditor
	{
		private static readonly AboutInfo s_aboutInfo = 
				new AboutInfo(s_aboutHelp, "UIFX - Fill Texture Filter\n© Chocolate Dinosaur Ltd", "uifx-logo-fill-filter")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/bundle/components/fill-texture-filter/"),
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

		private static readonly GUIContent Content_Scale = new GUIContent("Scale");
		private static readonly GUIContent Content_Rotate = new GUIContent("Rotate");
		private static readonly GUIContent Content_Offset = new GUIContent("Offset");
		private static readonly GUIContent Content_FitMode = new GUIContent("Fit Mode");
		private static readonly GUIContent Content_WrapMode = new GUIContent("Wrap Mode");
		private static readonly GUIContent Content_Transform = new GUIContent("Transform");

		private SerializedProperty _propTexture;
		private SerializedProperty _propTextureScaleMode;
		private SerializedProperty _propTextureWrapMode;
		private SerializedProperty _propColor;
		private SerializedProperty _propTextureRotate;
		private SerializedProperty _propTextureScale;
		private SerializedProperty _propTextureOffset;
		private SerializedProperty _propScrollSpeed;
		//private SerializedProperty _propTextureScale;
		private SerializedProperty _propBlendMode;
		private SerializedProperty _propStrength;
		private SerializedProperty _propRenderSpace;

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
    }
}