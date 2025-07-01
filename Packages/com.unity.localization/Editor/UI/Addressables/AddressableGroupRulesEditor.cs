using UnityEditor.Localization.Addressables;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.UI.Addressables
{
    [CustomEditor(typeof(AddressableGroupRules))]
    class AddressableGroupRulesEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = Resources.GetTemplate(nameof(AddressableGroupRulesEditor));

            root.Insert(0, new IMGUIContainer(() =>
            {
                if (target == AddressableGroupRules.Instance) return;
                EditorGUILayout.HelpBox("This asset is not currently the active Addressables Rules.", MessageType.Info);
                if (GUILayout.Button("Make Active"))
                {
                    AddressableGroupRules.Instance = target as AddressableGroupRules;
                }
            }));

            return root;
        }
    }
}
