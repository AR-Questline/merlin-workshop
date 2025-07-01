using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Main.UI.Helpers {
    public class OnlyOnChosenScenes : MonoBehaviour {
        public string[] scenes = Array.Empty<string>();

        void Start() {
            Scene scene = gameObject.scene;
            bool active = scenes.Contains(scene.name);
            gameObject.SetActive(active);
        }
    }
}