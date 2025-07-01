using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.UI
{
    [CustomEditor(typeof(VariablesGroupAsset), true)]
    class GlobalVariableGroupEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI() => new GlobalVariableGroupList(serializedObject.FindProperty("m_Variables"), typeof(IVariable));
    }
}
