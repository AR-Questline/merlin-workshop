// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    [CustomPropertyDrawer(typeof(SharePreBuildData))]
    public class SharePreBuildDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return default;
        }
    }
}
