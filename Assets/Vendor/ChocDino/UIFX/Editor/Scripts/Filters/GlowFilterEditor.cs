//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(GlowFilter), true)]
	[CanEditMultipleObjects]
	internal class GlowFilterEditor : FilterBaseEditor
	{

		private static readonly AboutInfo s_aboutInfo = 
				new AboutInfo(s_aboutHelp, "UIFX - Glow Filter\n© Chocolate Dinosaur Ltd", "uifx-logo-glow-filter")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("User Guide", "https://www.chocdino.com/products/uifx/glow-filter/about/"),
								new AboutButton("Scripting Guide", "https://www.chocdino.com/products/uifx/glow-filter/scripting/"),
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/glow-filter/components/glow-filter/"),
								new AboutButton("API Reference", "https://www.chocdino.com/products/uifx/glow-filter/API/ChocDino.UIFX/"),
							}
						},
						new AboutSection("Unity Asset Store Review\r\n<color=#ffd700>★★★★☆</color>")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Review <b>UIFX - Glow Filter</b>", "https://assetstore.unity.com/packages/slug/274847?aid=1100lSvNe#reviews"),
								new AboutButton("Review <b>UIFX Bundle</b>", AssetStoreBundleReviewUrl),
							}
						},
						new AboutSection("UIFX Support")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Discord Community", DiscordUrl),
								new AboutButton("Post to Unity Discussions", ""),
								new AboutButton("Post Issues to GitHub", GithubUrl),
								new AboutButton("Email Us", SupportEmailUrl),
							}
						}
					}
				};

		private static readonly AboutToolbar s_aboutToolbar = new AboutToolbar(new AboutInfo[] { s_upgradeToBundle, s_aboutInfo } );

		private static readonly GUIContent Content_Shape = new GUIContent("Shape");
		private static readonly GUIContent Content_Falloff = new GUIContent("Falloff");
		private static readonly GUIContent Content_Distance = new GUIContent("Distance");
		private static readonly GUIContent Content_Mode = new GUIContent("Mode");
		private static readonly GUIContent Content_Curve = new GUIContent("Curve");
		private static readonly GUIContent Content_Energy = new GUIContent("Energy");
		private static readonly GUIContent Content_Power = new GUIContent("Power");
		private static readonly GUIContent Content_Gamma = new GUIContent("Gamma");
		private static readonly GUIContent Content_Offset = new GUIContent("Offset");

		private SerializedProperty _propEdgeSide;
		private SerializedProperty _propDistanceShape;
		private SerializedProperty _propMaxDistance;
		private SerializedProperty _propReuseDistanceMap;
		private SerializedProperty _propFalloffMode;
		private SerializedProperty _propExpFalloffEnergy;
		private SerializedProperty _propExpFalloffPower;
		private SerializedProperty _propExpFalloffOffset;
		private SerializedProperty _propFalloffCurve;
		private SerializedProperty _propFalloffCurveGamma;
		private SerializedProperty _propFillMode;
		private SerializedProperty _propColor;
		private SerializedProperty _propGradient;
		private SerializedProperty _propGradientTexture;
		private SerializedProperty _propGradientOffset;
		private SerializedProperty _propGradientGamma;
		private SerializedProperty _propGradientReverse;
		private SerializedProperty _propBlur;
		private SerializedProperty _propSourceAlpha;
		private SerializedProperty _propAdditive;
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