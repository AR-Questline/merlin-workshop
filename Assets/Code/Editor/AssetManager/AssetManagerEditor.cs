using Awaken.TG.Graphics.AssetManager;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using Unity.VisualScripting.ReorderableList.Internal;
using UnityEditor;

namespace Awaken.TG.Editor.AssetManager {
    public class AssetManagerEditor : OdinMenuEditorWindow {
        [MenuItem("TG/AssetManager")]
        static void Init() {
            var window = GetWindow<AssetManagerEditor>();
            //window.position = GUIHelper.GetEditorWindowRect().AlignCenter(800, 600);
        }

        // protected override OdinMenuTree BuildMenuTree() {
        //     var tree = new OdinMenuTree();
        //     
        //     tree.Add("Characters", new AssetManagerCharacters());
        //     // tree.Add("VFX", new AssetManagerVFXCreate());
        //     // tree.Add("VFX/List", new AssetManagerVFX());
        //
        //     return tree;
        // }
    }
}
// Sortowanie list (nie tylko po spawnie)
// Odświeżanie VFXTemplate przy selekcji (nie po spawnie)
// Automatyczny import property z VFXGraph i Particle Systems
// Dodać zakładkę Validate do template'ów
// Manualne cache dla list