#if UNITY_EDITOR && ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using UnityEditor;
using UnityEngine;
using Unity.Rendering.Occlusion;

[CustomEditor(typeof(OcclusionView))]
[CanEditMultipleObjects]
public class OcclusionViewEditor : Editor
{
    SerializedProperty enabled;
    SerializedProperty width;
    SerializedProperty height;
    string displayWarning;

    void OnEnable()
    {
        enabled = serializedObject.FindProperty("OcclusionEnabled");
        width = serializedObject.FindProperty("OcclusionBufferWidth");
        height = serializedObject.FindProperty("OcclusionBufferHeight");
        displayWarning = "";
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        int oldWidth = width.intValue;
        int oldHeight = height.intValue;

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(enabled);
        EditorGUILayout.DelayedIntField(width);
        EditorGUILayout.DelayedIntField(height);

        if (displayWarning != "")
        {
            EditorGUILayout.HelpBox(displayWarning, MessageType.Warning);
        }

        if (EditorGUI.EndChangeCheck())
        {
            displayWarning = "";

            // TODO:  The valid multiples are dependent on the tile count per bin, which is currently
            // hardcoded to (2,4).  Fix this once that's driven by the view settings.
            if (width.intValue <= 0 || width.intValue % 64 != 0)
            {
                InvalidDimensionWarning("width", width.intValue, 64);
                width.intValue = oldWidth;
            }

            if (height.intValue <= 0 || height.intValue % 64 != 0)
            {
                InvalidDimensionWarning("height", height.intValue, 64);
                height.intValue = oldHeight;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    private void InvalidDimensionWarning(string dimName, int dim, int mul)
    {
        displayWarning = $"Invalid occlusion buffer {dimName} = {dim}.  The {dimName} must be a multiple of {mul} greater than zero.";
    }
}

#endif
