using System;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Settings.Controllers.Switchers {
    [Serializable]
    public class MaterialSwitcher : IControllerSwitcher {
        public Material material;

        public void Refresh(bool enabled, GameObject go) {
            if (enabled) {
                MeshRenderer renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null) {
                    renderer.material = material;
                } else {
                    Log.Important?.Error($"MaterialSwitcher attached to GameObject without MeshRenderer {go.name}", go);
                }
            }
        }
    }
}