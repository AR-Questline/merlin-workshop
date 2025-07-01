using System;
using UnityEngine;

namespace Awaken.TG.Main.Settings.Controllers.Switchers {
    [Serializable]
    public class EnabledSwitcher : IControllerSwitcher {
        public void Refresh(bool enabled, GameObject go) {
            go.SetActive(enabled);
        }
    }
}