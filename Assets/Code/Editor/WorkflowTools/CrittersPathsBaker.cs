using Awaken.TG.Main.Locations.Spawners.Critters;
using Awaken.Utility.Editor.Scenes;
using Awaken.Utility.GameObjects;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Editor.WorkflowTools {
    public class CrittersPathsBaker : SceneProcessor {
        public override int callbackOrder => 0;
        public override bool canProcessSceneInIsolation => false;
        protected override void OnProcessScene(Scene scene, bool processingInPlaymode) {
            var critterSpawnerAttachments = GameObjects.FindComponentsByTypeInScene<CritterSpawnerAttachment>(scene, true, 4);
            foreach (var critterSpawnerAttachment in critterSpawnerAttachments) {
                critterSpawnerAttachment.GeneratePaths();
                EditorUtility.SetDirty(critterSpawnerAttachment);
            }
        }
    }
}