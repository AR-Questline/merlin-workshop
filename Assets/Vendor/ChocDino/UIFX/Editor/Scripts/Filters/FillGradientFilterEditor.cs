//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(FillGradientFilter), true)]
	[CanEditMultipleObjects]
	internal class FillGradientFilterEditor : FilterBaseEditor
	{
		private static readonly AboutInfo s_aboutInfo = 
				new AboutInfo(s_aboutHelp, "UIFX - Fill Gradient Filter\n© Chocolate Dinosaur Ltd", "uifx-logo-fill-filter")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/bundle/components/fill-gradient-filter/"),
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

		private static readonly GUIContent Content_Radius = new GUIContent("Radius");
		private static readonly GUIContent Content_Scale = new GUIContent("Repeat");
		private static readonly GUIContent Content_ScaleCenter = new GUIContent("Repeat Pivot");
		private static readonly GUIContent Content_CenterX = new GUIContent("Center X");
		private static readonly GUIContent Content_CenterY = new GUIContent("Center Y");
		private static readonly GUIContent Content_Offset = new GUIContent("Offset");
		private static readonly GUIContent Content_Angle = new GUIContent("Angle");
		private static readonly GUIContent Content_Wrap = new GUIContent("Wrap");
		private static readonly GUIContent Content_Shape = new GUIContent("Shape");
		private static readonly GUIContent Content_Dither = new GUIContent("Dither");
		private static readonly GUIContent Content_Transform = new GUIContent("Transform");
		private static readonly GUIContent Content_ColorLerp = new GUIContent("Color Lerp");
		private static readonly GUIContent Content_ColorSpace = new GUIContent("ColorSpace");

		private SerializedProperty _propGradient;
		private SerializedProperty _propGradientShape;
		private SerializedProperty _propDiagonalFlip;
		private SerializedProperty _propGradientCenterX;
		private SerializedProperty _propGradientCenterY;
		private SerializedProperty _propGradientRadius;
		private SerializedProperty _propGradientLinearAngle;
		private SerializedProperty _propGradientScale;
		private SerializedProperty _propGradientScaleCenter;
		private SerializedProperty _propGradientOffset;
		private SerializedProperty _propScrollSpeed;
		private SerializedProperty _propGradientWrap;
		private SerializedProperty _propGradientColorSpace;
		private SerializedProperty _propGradientLerp;
		private SerializedProperty _propGradientDither;
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