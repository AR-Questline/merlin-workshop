using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Awaken.ECS.Editor {
    public static class PrefabsHelper {
        public static bool IsLowestEditablePrefabStage(MonoBehaviour target, bool editPrefabContents = false) {
            if (!editPrefabContents) {
                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (!prefabStage) {
                    return false;
                }
            }
            var outermost = PrefabUtility.GetOutermostPrefabInstanceRoot(target);
            if (!outermost) {
                return true;
            }

            if (PrefabUtility.IsPartOfImmutablePrefab(outermost)) {
                outermost = PrefabUtility.GetNearestPrefabInstanceRoot(target);
                return PrefabUtility.GetOutermostPrefabInstanceRoot(outermost.transform.parent) == null;
            }
            return false;
        }
    }
}
