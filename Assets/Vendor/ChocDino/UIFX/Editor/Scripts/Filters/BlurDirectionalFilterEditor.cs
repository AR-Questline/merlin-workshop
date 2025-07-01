//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(BlurDirectionalFilter), true)]
	[CanEditMultipleObjects]
	internal class BlurDirectionalFilterEditor : FilterBaseEditor
	{
		private static readonly AboutToolbar s_aboutToolbar = new AboutToolbar(new AboutInfo[] { s_upgradeToBundle, BlurFilterEditor.s_aboutInfo } );

		private static readonly GUIContent Content_FadeCurve = new GUIContent("Fade Curve");
		private static readonly GUIContent Content_Blur = new GUIContent("Blur");

		private SerializedProperty _propAngle;
		private SerializedProperty _propLength;
		private SerializedProperty _propSide;
		private SerializedProperty _propWeights;
		private SerializedProperty _propWeightsPower;
		private SerializedProperty _propDither;
		private SerializedProperty _propApplyAlphaCurve;
		private SerializedProperty _propAlphaCurve;
		private SerializedProperty _propTintColor;
		private SerializedProperty _propPower;
		private SerializedProperty _propIntensity;
		private SerializedProperty _propBlend;
		private SerializedProperty _propStrength;
		private SerializedProperty _propRenderSpace;
		private SerializedProperty _propExpand;

		protected virtual void OnEnable()
        {
        }

        public override void OnInspectorGUI()
        {
        }
    }
}