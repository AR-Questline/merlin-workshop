using Awaken.TG.Main.Heroes.SkinnedBones;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.FBX {
    public class ClothStitcherWindow : OdinEditorWindow {
        [SerializeField] GameObject avatar;
        [SerializeField] GameObject cloth;
        [SerializeField] bool withoutAdditionalBonesCatalogue;
        
        [MenuItem("TG/Assets/Mesh/Cloth Stitcher")]
        static void ShowWindow() {
            EditorWindow.GetWindow<ClothStitcherWindow>().Show();
        }

        [Button]
        void Stitch() {
            ClothStitcher.Stitch(cloth, avatar, withBonesCatalogue: !withoutAdditionalBonesCatalogue);
        }
    }
}