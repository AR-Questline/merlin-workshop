//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(TrailEffectBase), true)]
	[CanEditMultipleObjects]
	internal class TrailEffectEditor : BaseEditor
	{
		static readonly GUIContent Content_Trail = new GUIContent("Trail");
		static readonly GUIContent Content_Layers = new GUIContent("Layers");
		static readonly GUIContent Content_Gradient = new GUIContent("Gradient");
		static readonly GUIContent Content_Offset = new GUIContent("Offset");
		static readonly GUIContent Content_Scale = new GUIContent("Scale");
		static readonly GUIContent Content_Animation = new GUIContent("Animation");
		static readonly GUIContent Content_OffsetSpeed = new GUIContent("Offset Speed");
		static readonly GUIContent Content_Apply = new GUIContent("Apply");
		static readonly GUIContent Content_Mode = new GUIContent("Vertex Modifier");

		private SerializedProperty _propLayerCount;
		private SerializedProperty _propDampingFront;
		private SerializedProperty _propDampingBack;
		private SerializedProperty _propAlphaCurve;
		private SerializedProperty _propVertexModifierSource;
		private SerializedProperty _propGradient;
		private SerializedProperty _propGradientOffset;
		private SerializedProperty _propGradientScale;
		private SerializedProperty _propGradientOffsetSpeed;
		private SerializedProperty _propShowTrailOnly;
		private SerializedProperty _propBlendMode;
		private SerializedProperty _propStrengthMode;
		private SerializedProperty _propStrength;

		private static readonly AboutInfo s_aboutInfo =
				new AboutInfo(s_aboutHelp, "UIFX - Trail Effect\n© Chocolate Dinosaur Ltd", "uifx-logo-trail-effect")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("User Guide", "https://www.chocdino.com/products/uifx/trail/about/"),
								new AboutButton("Scripting Guide", "https://www.chocdino.com/products/uifx/trail/scripting/"),
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/trail/components/trail-effect/"),
								new AboutButton("API Reference", "https://www.chocdino.com/products/uifx/trail/API/ChocDino.UIFX/"),
							}
						},
						new AboutSection("Unity Asset Store Review\r\n<color=#ffd700>★★★★☆</color>")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Review <b>UIFX - Trail Effect</b>", "https://assetstore.unity.com/packages/slug/260697?aid=1100lSvNe#reviews"),
								new AboutButton("Review <b>UIFX Bundle</b>", AssetStoreBundleReviewUrl),
							}
						},
						new AboutSection("UIFX Support")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Discord Community", DiscordUrl),
								new AboutButton("Post to Unity Forum Thread", "https://discussions.unity.com/t/released-uifx-trail-effect/930438"),
								new AboutButton("Post Issues to GitHub", GithubUrl),
								new AboutButton("Email Us", SupportEmailUrl),
							}
						}
					}
				};

		private static readonly AboutToolbar s_aboutToolbar = new AboutToolbar(new AboutInfo[] { s_upgradeToBundle, s_aboutInfo } );

		void OnEnable()
        {
        }

        public override void OnInspectorGUI()
        {
        }
    }
}