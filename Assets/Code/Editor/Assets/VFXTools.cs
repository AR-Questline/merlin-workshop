using Awaken.TG.Assets;
using Awaken.TG.Graphics.VFX.IndirectSamplingUniform;
using Awaken.TG.MVC;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Assets {
    public static class VFXTools {
        [MenuItem("ArtTools/VFX/Clear Prefab Pool")]
        static void ClearPrefabPool() {
            World.Services?.TryGet<PrefabPool>()?.RemoveOldReferences(forceClean: true);
        }
    }
}