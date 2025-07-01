using System;
using Awaken.Utility.Graphics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Settings.Controllers.Switchers {
    [Serializable]
    public class SpaceEmissionSwitcher : IControllerSwitcher {
        public float limit = 250_000f;
        
        public void Refresh(bool enabled, GameObject go) {
            var volume = go.GetComponent<Volume>();
            if (!enabled && volume.TryGetVolumeComponent(out PhysicallyBasedSky sky)) {
                float spaceEmission = sky.spaceEmissionMultiplier.value;
                spaceEmission = Mathf.Min(spaceEmission, limit);
                sky.spaceEmissionMultiplier.value = spaceEmission;
            }
        }
    }
}