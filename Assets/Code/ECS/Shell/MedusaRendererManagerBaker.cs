using System;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using Awaken.Utility.Editor.Scenes;
#endif

namespace Awaken.ECS.Editor.MedusaRenderer {
    public class MedusaRendererManagerBaker
#if UNITY_EDITOR
        : SceneProcessor
#endif
    {
        public
#if UNITY_EDITOR
            override
#endif
            int callbackOrder => throw new NotImplementedException();

        public
#if UNITY_EDITOR
            override
#endif
            bool canProcessSceneInIsolation => throw new NotImplementedException();

        public static void ClearMedusaLibraryAssets() {
            throw new NotImplementedException();
        }

        protected
#if UNITY_EDITOR
            override
#endif
            void OnProcessScene(Scene scene, bool processingInPlaymode) {
            throw new NotImplementedException();
        }
    }
}