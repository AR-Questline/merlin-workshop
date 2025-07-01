using UnityEngine;

namespace Awaken.TG.Main.Heroes.HUD.Bars {
    public abstract class Bar : MonoBehaviour {
        public abstract void SetPercent(float percent);
        public virtual void SetPercentInstant(float percent) {
            SetPercent(percent);
        }
        public virtual void SetPrediction(float percent) { }
        public abstract Color Color { get; set; }
    }
}