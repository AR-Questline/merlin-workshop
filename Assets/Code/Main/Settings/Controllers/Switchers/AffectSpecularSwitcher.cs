using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Settings.Controllers.Switchers {
    [Serializable]
    public class AffectSpecularSwitcher : IControllerSwitcher {
        public void Refresh(bool enabled, GameObject go) {
            HDAdditionalLightData lightData = go?.GetComponent<HDAdditionalLightData>();
            if (lightData == null) {
                UnityEngine.Debug.LogException(new NullReferenceException($"Null in switcher: {go}"), go);
            } else {
                lightData.affectSpecular = enabled;
            }
        }
    }
}