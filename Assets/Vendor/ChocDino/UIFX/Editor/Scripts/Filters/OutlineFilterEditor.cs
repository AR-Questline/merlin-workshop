//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	/*
		internal class GradientShaderEditor
	{
		internal SerializedProperty _propGradient;
		internal SerializedProperty _propMixMode;
		internal SerializedProperty _propColorSpace;
		internal SerializedProperty _propDither;
		internal SerializedProperty _propScale;
		internal SerializedProperty _propScaleCenter;
		internal SerializedProperty _propOffset;
		internal SerializedProperty _propWrapMode;

		public GradientShaderEditor(SerializedProperty parent)
		{
			_propGradient = BaseEditor.VerifyFindPropertyRelative(parent, "_gradient");
			_propMixMode = BaseEditor.VerifyFindPropertyRelative(parent, "_mixMode");
			_propColorSpace = BaseEditor.VerifyFindPropertyRelative(parent, "_colorSpace");
			_propDither = BaseEditor.VerifyFindPropertyRelative(parent, "_dither");
			_propScale = BaseEditor.VerifyFindPropertyRelative(parent, "_scale");
			_propScaleCenter = BaseEditor.VerifyFindPropertyRelative(parent,"_scalePivot");
			_propOffset = BaseEditor.VerifyFindPropertyRelative(parent,"_offset");
			_propWrapMode = BaseEditor.VerifyFindPropertyRelative(parent,"_wrapMode");
		}
	}*/

	[CustomEditor(typeof(OutlineFilter), true)]
	[CanEditMultipleObjects]
	internal class OutlineFilterEditor : FilterBaseEditor
	{
		private static readonly AboutInfo s_aboutInfo = 
				new AboutInfo(s_aboutHelp, "UIFX - Outline Filter\n© Chocolate Dinosaur Ltd", "uifx-logo-outline-filter")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("User Guide", "https://www.chocdino.com/products/uifx/outline-filter/about/"),
								new AboutButton("Scripting Guide", "https://www.chocdino.com/products/uifx/outline-filter/scripting/"),
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/outline-filter/components/outline-filter/"),
								new AboutButton("API Reference", "https://www.chocdino.com/products/uifx/outline-filter/API/ChocDino.UIFX/"),
							}
						},
						new AboutSection("Unity Asset Store Review\r\n<color=#ffd700>★★★★☆</color>")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Review <b>UIFX - Outline Filter</b>", "https://assetstore.unity.com/packages/slug/273578?aid=1100lSvNe#reviews"),
								new AboutButton("Review <b>UIFX Bundle</b>", AssetStoreBundleReviewUrl),
							}
						},
						new AboutSection("UIFX Support")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Discord Community", DiscordUrl),
								new AboutButton("Post to Unity Discussions", "https://discussions.unity.com/t/released-uifx-outline-filter/940939"),
								new AboutButton("Post Issues to GitHub", GithubUrl),
								new AboutButton("Email Us", SupportEmailUrl),
							}
						}
					}
				};

		private static readonly AboutToolbar s_aboutToolbar = new AboutToolbar(new AboutInfo[] { s_upgradeToBundle, s_aboutInfo } );

		private static readonly GUIContent Content_Distance = new GUIContent("Distance");
		private static readonly GUIContent Content_Shape = new GUIContent("Shape");
		private static readonly GUIContent Content_Outline = new GUIContent("Outline");

		//private GradientShaderEditor _gradientEditor;

		private SerializedProperty _propMethod;
		private SerializedProperty _propSize;
		//private SerializedProperty _propMaxSize;
		private SerializedProperty _propDirection;
		private SerializedProperty _propDistanceShape;
		private SerializedProperty _propBlur;
		private SerializedProperty _propSoftness;
		private SerializedProperty _propColor;
		//private SerializedProperty _propGradient;
		//private SerializedProperty _propTexture;
		//private SerializedProperty _propTextureOffset;
		//private SerializedProperty _propTextureScale;
		private SerializedProperty _propSourceAlpha;
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