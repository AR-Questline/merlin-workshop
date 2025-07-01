using System;
using Awaken.TG.Assets;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.General.Caches {
    [Serializable]
    public abstract class SceneDataSources {
        [HideInInspector]
        public SceneReference sceneRef;

        string _sceneName;
        [ShowInInspector, PropertyOrder(0)] public string SceneName => string.IsNullOrEmpty(_sceneName) ? _sceneName = sceneRef.Name : _sceneName;

        protected SceneDataSources(SceneReference sceneRef) {
            this.sceneRef = sceneRef;
        }
    }
}