using System.Collections.Generic;
using Awaken.TG.Code.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Utility.VFX {
    /// <summary>
    /// Class used in VFXs prefabs to enable randomly additional effects
    /// </summary>
    public class ActivateRandomGameObjects : MonoBehaviour {
        [Range(1, 10)] public int activateCount = 1;
        public List<GameObject> gameObjectToActivate;

        void OnEnable() {
            var result = RandomUtil.UniformSelectMultiple(gameObjectToActivate, activateCount);
            foreach (var go in result) {
                go.SetActive(true);
            }
        }

        void OnDisable() {
            gameObjectToActivate.ForEach(go => go.SetActive(false));
        }
    }
}
