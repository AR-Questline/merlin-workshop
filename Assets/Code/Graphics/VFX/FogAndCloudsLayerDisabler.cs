using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Graphics.VFX {
    [ExecuteInEditMode]
    public class FogAndCloudsLayerDisabler : MonoBehaviour {

        void OnEnable() {
            SetFogAndCloudsEnabled(false);
        }

        void OnDisable() {
            SetFogAndCloudsEnabled(true);
        }
        
        void Start() {
            SetFogAndCloudsEnabled(!gameObject.activeInHierarchy);
        }

        public static void SetFogAndCloudsEnabled(bool enable) {
            var volumes = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);

            foreach (var volume in volumes) {
                VolumeProfile profile = volume.sharedProfile;
                if (profile != null) {
                    if (profile.TryGet<Fog>(out var fog)) {
                        fog.active = enable;
                    }
                    if (profile.TryGet<CloudLayer>(out var cloudLayer)) {
                        cloudLayer.active = enable;
                    }
                }
            }
        }
    }
}