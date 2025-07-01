// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// CheckSliderSerializeDataプロパティのカスタムGUI描画
    /// </summary>
    [CustomPropertyDrawer(typeof(CheckSliderSerializeData))]
    public class CheckSliderSerializeDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
        }
    }
}
