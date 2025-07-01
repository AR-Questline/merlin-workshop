using System.Reflection;
using Awaken.TG.Assets;

namespace Awaken.TG.Editor.Assets {
    public static class SceneReferenceEditorUtils {
        static readonly FieldInfo SceneReferenceReferenceFieldInfo = typeof(SceneReference).GetField("reference", BindingFlags.NonPublic | BindingFlags.Instance);
        
        public static bool TryGetSceneAssetGUID(this SceneReference sceneRef, out string guid) {
            var assetRef = SceneReferenceReferenceFieldInfo.GetValue(sceneRef) as ARAssetReference;
            guid = assetRef?.Address;
            return !string.IsNullOrWhiteSpace(guid);
        }
    }
}