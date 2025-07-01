#if UNITY_EDITOR && !ADDRESSABLES_BUILD
using UnityEditor;
using UnityEditor.ShortcutManagement;
#endif
using Awaken.CommonInterfaces.Assets;
using Awaken.TG.Assets;
using Awaken.Utility.Assets;
using UnityEngine;

namespace Awaken.TG.EditorOnly.WorkflowTools {
    [DisallowMultipleComponent]
    public class PrioritizeSelection : MonoBehaviour, IEditorOnlyMonoBehaviour {
#if UNITY_EDITOR && !ADDRESSABLES_BUILD
        [Shortcut("Tools/Select Priority Selection In Parents", KeyCode.BackQuote, ShortcutModifiers.Action)]
        public static void SelectPrioritySelectionInParents() {
            var go = Selection.activeGameObject;
            if (go == null) return;

            var ps = go.GetComponentInParent<PrioritizeSelection>();
            if (ps == null) return;
            
            Selection.activeGameObject = ps.gameObject;
        }
#endif
    }
}