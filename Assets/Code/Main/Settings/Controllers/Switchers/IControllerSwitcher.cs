using UnityEngine;

namespace Awaken.TG.Main.Settings.Controllers.Switchers {
    public interface IControllerSwitcher {
        void Refresh(bool enabled, GameObject go);
    }
}