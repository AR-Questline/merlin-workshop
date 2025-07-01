using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class InvertedValueDrawer : MaterialPropertyDrawer
{
    public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor) {
        EditorGUI.BeginChangeCheck();

        var value = 1f / EditorGUI.FloatField(position, label, 1f / prop.floatValue);
        EditorGUILayout.LabelField("Real value: " + value);

        if (EditorGUI.EndChangeCheck())
        {
            prop.floatValue = value;
        }
    }
}
