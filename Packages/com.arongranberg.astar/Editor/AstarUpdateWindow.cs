using System;
using UnityEditor;
using UnityEngine;

namespace Pathfinding {
	public class AstarUpdateWindow : EditorWindow {
		static GUIStyle largeStyle;
		static GUIStyle normalStyle;
		Version version;
		string summary;
		bool setReminder;

		public static AstarUpdateWindow Init (Version version, string summary) {
            return default;
        }

        public void OnDestroy()
        {
        }

        void OnGUI()
        {
        }
    }
}
