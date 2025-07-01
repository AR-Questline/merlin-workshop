//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	internal struct AboutButton
	{
		public GUIContent title;
		public string url;

		public AboutButton(string title, string url) : this()
        {
        }
    }

	internal struct AboutSection
	{
		public GUIContent title;
		public AboutButton[] buttons;

		public AboutSection(string title) : this()
        {
        }
    }

	internal class AboutToolbar
	{
		private int _selectedIndex = -1;
		private AboutInfo[] _infos;

		public AboutToolbar(AboutInfo[] infos)
        {
        }

        internal void OnGUI()
        {
        }
    }

	/// <summary>
	/// A customisable UI window that displays basic information about the asset/component and has
	/// categories of buttons that link to documentation and support
	/// </summary>
	internal class AboutInfo
	{
		private string iconPath;
		private GUIContent title;
		internal AboutSection[] sections;
		private GUIContent icon;
		private GUIContent buttonLabelOpen;
		private GUIContent buttonLabelClosed;
		private System.Predicate<bool> showAction;

		internal bool isExpanded = false;
		private static GUIStyle paddedBoxStyle;
		private static GUIStyle richBoldLabelStyle;
		private static GUIStyle richButtonStyle;
		private static GUIStyle titleStyle;

		public AboutInfo(string buttonLabel, string title, string iconPath, System.Predicate<bool> showAction = null)
        {
        }

        internal bool Visible()
        {
            return default;
        }

        internal bool OnHeaderGUI()
        {
            return default;
        }

        internal void OnGUI()
        {
        }
    }
}
