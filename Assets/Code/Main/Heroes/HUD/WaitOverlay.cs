using System;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.HUD {
    public class WaitOverlay : MonoBehaviour {
        public Image[] overlays = Array.Empty<Image>();
        public float hardness = 0.7f;

        public float Progress { get; private set; }

        void Awake() {
            Array.ForEach(overlays, o => {
                Color c = o.color;
                c.a = hardness;
                o.color = c;
            });
        }

        public void Set(float amount) {
            Progress = amount;
            Array.ForEach(overlays, o => o.fillAmount = Progress);
        }
    }
}
