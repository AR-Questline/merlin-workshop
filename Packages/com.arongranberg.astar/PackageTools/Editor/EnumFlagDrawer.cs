using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Pathfinding {
	[CustomPropertyDrawer(typeof(EnumFlagAttribute))]
	public class EnumFlagDrawer : PropertyDrawer {
		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
        }

        static T GetBaseProperty<T>(SerializedProperty prop) {
            return default;
        }
    }
}
