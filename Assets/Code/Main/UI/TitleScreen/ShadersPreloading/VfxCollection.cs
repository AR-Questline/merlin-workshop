using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.UI.TitleScreen.ShadersPreloading {
    [CreateAssetMenu(menuName = "TG/Assets/Shaders/Vfx collection")]
    public class VfxCollection : ScriptableObject {
        public GameObject[] vfxPrefabs = Array.Empty<GameObject>();
        public VisualEffectAsset[] vfxAssets = Array.Empty<VisualEffectAsset>();

#if UNITY_EDITOR
        [Button]
        void CollectVfxs(bool update) {
            var vfxNewSet = new HashSet<GameObject>();
            if (update) {
                vfxNewSet.UnionWith(vfxPrefabs.WhereNotUnityNull());
            }

            var allPrefabsGuids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            foreach (var guid in allPrefabsGuids) {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.GetComponent<VisualEffect>() != null) {
                    vfxNewSet.Add(prefab);
                }
            }

            vfxPrefabs = vfxNewSet.ToArray();

            var vfxAssetsSet = new HashSet<VisualEffectAsset>();
            if (update) {
                vfxAssetsSet.UnionWith(vfxAssets.WhereNotUnityNull());
            }

            var allAssetsGuids = UnityEditor.AssetDatabase.FindAssets("t:VisualEffectAsset", new[] { "Assets" });
            foreach (var guid in allAssetsGuids) {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var visualEffectAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
                if (visualEffectAsset != null) {
                    vfxAssetsSet.Add(visualEffectAsset);
                }
            }

            vfxAssets = vfxAssetsSet.ToArray();
        }
#endif
    }
}
