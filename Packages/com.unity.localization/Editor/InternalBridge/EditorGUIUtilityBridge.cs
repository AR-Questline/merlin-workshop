using System;
using UnityEngine;

namespace UnityEditor.Localization.Bridge
{
    internal class EditorGUIUtilityBridge
    {
        #if UNITY_2020_2_OR_NEWER
        public static event Action<Rect, SerializedProperty> beginProperty
        {
            add => EditorGUIUtility.beginProperty += value;
            remove => EditorGUIUtility.beginProperty -= value;
        }
        #endif
    }
}
