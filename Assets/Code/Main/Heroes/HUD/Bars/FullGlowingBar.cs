using Awaken.TG.Main.Timing.ARTime;
using Awaken.Utility.Maths.Data;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.HUD.Bars {
    public class FullGlowingBar : Bar {
        [SerializeField] Bar filledBar;
        [Space]
        [SerializeField] float fillSpeed;
        [Space]
        [SerializeField] GlowEffect fullGlowEffect;
        
        DelayedValue _fillPercent;
        bool _wasFull;
        float DeltaTime => Hero.Current.GetDeltaTime();

        public override Color Color { get; set; }

        void Awake() {
            fullGlowEffect.Init();
        }

        void Update() {
            if (Hero.Current is { WasDiscarded: false }) {
                _fillPercent.Update(DeltaTime, fillSpeed);
                filledBar.SetPercent(_fillPercent.Value);
            }
        }

        public override void SetPercent(float percent) {
            if (percent >= 1 - float.Epsilon && !_wasFull) {
                _wasFull = true;
                FullGlow();
            }

            if (percent < 1 - float.Epsilon && _wasFull) {
                _wasFull = false;
                StopGlow();
            }           
            
            _fillPercent.Set(percent);
        }

        void StopGlow() {
            fullGlowEffect.StopGlow();
        }
        
        void FullGlow() {
            fullGlowEffect.StartGlow();
        }
    }
}