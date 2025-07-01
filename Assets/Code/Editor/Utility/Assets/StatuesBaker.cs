using Awaken.TG.Editor.Graphics.Statues;
using Awaken.TG.Graphics.Statues;
using Awaken.Utility.Editor.Scenes;
using Awaken.Utility.GameObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Editor.Utility.Assets {
    public class StatuesBaker : SceneProcessor {
        public override int callbackOrder => 0;
        public override bool canProcessSceneInIsolation => true;
        protected override void OnProcessScene(Scene scene, bool processingInPlaymode) {
            var statues = GameObjects.FindComponentsByTypeInScene<Statue>(scene, true);
            int count = statues.Count;
            for (int i = 0; i < count; i++) {
                var statue = statues[i];
                var statueAccess = new Statue.EditorAccess(statue);
                StatueEditor.RegenerateStaticModel(statueAccess, !processingInPlaymode);
                statueAccess.DoNotDestroyBakedStaticInstance = true;
                EditorUtility.SetDirty(statueAccess.RootTransform);
                Object.DestroyImmediate(statueAccess.Statue);
            }
        }
    }
}