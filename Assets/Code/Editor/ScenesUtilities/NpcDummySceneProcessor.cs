using Awaken.CommonInterfaces;
using Awaken.Kandra;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Setup;
using Awaken.Utility.Editor.Scenes;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace Awaken.TG.Editor.ScenesUtilities {
    [Preserve]
    public class NpcDummySceneProcessor : SceneProcessor {
        public override int callbackOrder => ProcessSceneOrder.NpcDummy;
        public override bool canProcessSceneInIsolation => true;
        protected override void OnProcessScene(Scene scene, bool processingInPlaymode) {
            foreach (var root in scene.GetRootGameObjects()) {
                var attachments = root.GetComponentsInChildren<NpcDummyAttachment>();
                foreach (var attachment in attachments) {
                    if (attachment.gameObject.isStatic && attachment.gameObject.GetComponent<LocationSpec>() is { IsHidableStatic: false } spec) {
                        spec.EDITOR_SetHideableStatic(true);
                        EditorUtility.SetDirty(spec);
                    }
                    var rigs = attachment.GetComponentsInChildren<KandraRig>();
                    foreach (var rig in rigs) {
                        rig.gameObject.AddComponent<NpcDummy.GameObjectToReenableMarker>();
                        rig.gameObject.SetActive(false);
                        EditorUtility.SetDirty(rig.gameObject);
                    }
                }
            }
        }
    }
}