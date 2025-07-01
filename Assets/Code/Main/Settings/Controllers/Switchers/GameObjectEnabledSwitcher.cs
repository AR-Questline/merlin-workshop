using System;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Settings.Controllers.Switchers {
    [Serializable]
    public class GameObjectEnabledSwitcher : IControllerSwitcher {
        public GameObject[] controlledObject;
        
        public void Refresh(bool enabled, GameObject _) {
            controlledObject?.ForEach(g => {
                if (g != null) {
                    g.SetActive(enabled);
                }
            });
        }
    }
}