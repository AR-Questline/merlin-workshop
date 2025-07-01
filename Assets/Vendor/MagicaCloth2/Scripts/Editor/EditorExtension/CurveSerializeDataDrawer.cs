// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// CurveSerializeDataプロパティのカスタムGUI描画
    /// </summary>
    [CustomPropertyDrawer(typeof(CurveSerializeData))]
    public class CurveSerializeDataDrawer : PropertyDrawer
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
