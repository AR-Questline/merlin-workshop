using Awaken.Kandra;
using Awaken.TG.Main.Heroes.SkinnedBones;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.FBX {
    public class ClothStitcherKandraWindow : OdinEditorWindow {
        [SerializeField] KandraRig rig;
        [SerializeField] GameObject cloth;
        
        [MenuItem("TG/Assets/Mesh/Cloth Stitcher - Kandra")]
        static void ShowWindow() {
            EditorWindow.GetWindow<ClothStitcherKandraWindow>().Show();
        }

        [Button]
        void Stitch() {
            ClothStitcher.Stitch(cloth, rig);
            rig.MarkAsBase();
            EditorUtility.SetDirty(rig);
        }
    }
}