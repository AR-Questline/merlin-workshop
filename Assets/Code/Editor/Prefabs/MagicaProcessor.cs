using MagicaCloth2;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Prefabs {
    public static class MagicaProcessor {
        static void ForEachMagica(Process process) {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab")) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset.GetComponentInChildren<MagicaCloth>()) {
                    var prefab = PrefabUtility.LoadPrefabContents(path);
                    try {
                        bool save = false;
                        process(prefab, ref save);
                        if (save) {
                            PrefabUtility.SaveAsPrefabAsset(prefab, path);
                        }
                    } catch (System.Exception e) {
                        Debug.LogException(e);
                    } finally {
                        PrefabUtility.UnloadPrefabContents(prefab);
                    }
                }
            }
        }
        
        [MenuItem("TG/Assets/Magica/Fix Pose Ratio")]
        static void FixPoseRatioAll() {
            ForEachMagica(FixPoseRatio);
        }
        
        static void FixPoseRatio(GameObject prefab, ref bool save) {
            foreach (var magica in prefab.GetComponentsInChildren<MagicaCloth>()) {
                if (magica.SerializeData.animationPoseRatio != 0) {
                    magica.SerializeData.animationPoseRatio = 0;
                    EditorUtility.SetDirty(magica);
                    save = true;
                }
            }
        }

        delegate void Process(GameObject prefab, ref bool save);
    }
}