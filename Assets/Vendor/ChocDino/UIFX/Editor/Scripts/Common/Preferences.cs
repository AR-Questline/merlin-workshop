//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ChocDino.UIFX.Editor
{
	internal static class Preferences
	{
		internal static readonly string SettingsPath = "Project/Chocolate Dinosaur/UIFX";

		[SettingsProvider]
		static SettingsProvider CreateSettingsProvider()
        {
            return default;
        }

        private class UIFXSettingsProvider : SettingsProvider
		{
			public UIFXSettingsProvider(string path, SettingsScope scope) : base(path, scope)
            {
            }

            private static readonly GUIContent Content_BakedImagesFolder = new GUIContent("Baked Images Folder", "Folder path for baked images. Path is relative to Assets folder");

			private string _defines;
			private string _oldDefines;
			private bool _unappliedChanges;
			private BuildTargetGroup _buildTarget;


			public override void OnActivate(string searchContext, VisualElement rootElement)
            {
            }

            public override void OnDeactivate()
            {
            }

            private void CacheDefines()
            {
            }

            private void ApplyDefines()
            {
            }

            private bool HasDefine(string define)
            {
                return default;
            }

            private void AddDefine(string define)
            {
            }

            private void RemoveDefine(string define)
            {
            }

            private bool HasDefineChanged(string define)
            {
                return default;
            }

            public override void OnTitleBarGUI()
            {
            }

            public override void OnFooterBarGUI()
            {
            }

            public override void OnGUI(string searchContext)
            {
            }

            private void Links()
            {
            }

            private bool ShowDefineToggle(string label, string define)
            {
                return default;
            }

            private void ShowEditorPref(GUIContent label, string prefKey, string defaultValue)
            {
            }
        }
	}
}