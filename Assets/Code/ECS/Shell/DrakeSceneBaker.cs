#if UNITY_EDITOR
using System;
using Awaken.Utility.Editor.Scenes;
using UnityEngine.SceneManagement;

namespace Awaken.ECS.Editor.DrakeRenderer {
    public class DrakeSceneBaker : SceneProcessor {
        public override int callbackOrder => throw new NotImplementedException();
        public override bool canProcessSceneInIsolation => throw new NotImplementedException();

        public static void ClearDrakeLibraryAssets() {
            throw new NotImplementedException();
        }

        protected override void OnProcessScene(Scene scene, bool processingInPlaymode) {
            throw new NotImplementedException();
        }
    }
}
#endif