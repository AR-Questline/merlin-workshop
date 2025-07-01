using UnityEngine;

namespace Awaken.TG.Main.Heroes.HUD.Bars {
    public class FillBarWithPrediction : Bar {

        // === References

        public Bar filled, prediction;

        // === State

        public float FillSize { get; private set; } = 1f;
        public float PredictionSize { get; private set; }
        
        public override Color Color { get; set; }

        // === Unity lifecycle
        
        void Update() {
            UpdateBars();
        }

        // === Updating

        public void SetState(float fill, float transition) {
            FillSize = fill;
            PredictionSize = transition;
        }

        public override void SetPercent(float percent) {
            SetState(percent, PredictionSize);
        }
        
        public override void SetPrediction(float percent) {
            SetState(FillSize, percent);
        }

        void UpdateBars() {
            filled.SetPercent(FillSize);
            prediction.SetPercent(PredictionSize);
        }
    }
}
