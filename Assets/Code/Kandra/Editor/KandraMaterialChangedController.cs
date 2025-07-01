using UnityEditor;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine;

namespace Awaken.Kandra.Editor {

    public static class KandraMaterialChangedController {
        [InitializeOnLoadMethod]
        static void Init() {
            HDShaderGUI.MaterialChanged += OnMaterialChanged;
        }

        static void OnMaterialChanged(Material material) {
            KandraRendererManager.Instance.MaterialBroker.Editor_OnMaterialChanged(material);
        }
    }
}