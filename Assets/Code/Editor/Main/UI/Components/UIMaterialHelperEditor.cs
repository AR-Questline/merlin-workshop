using System.Linq;
using Awaken.TG.Main.UI.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Editor.Main.UI.Components {
    [CustomEditor(typeof(UIMaterialHelper))]
    public class UIMaterialHelperEditor : UnityEditor.Editor {
        UIMaterialHelper Helper => (UIMaterialHelper) target;

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            if (GUILayout.Button("Assign all empty graphics")) {
                bool MaterialNotAssigned(Graphic g) => g.material == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(g.material));
                Helper.affectedGraphics = Helper.gameObject.GetComponentsInChildren<Graphic>().Where(MaterialNotAssigned).ToArray();
                EditorUtility.SetDirty(Helper);
            }
        }
    }
}
