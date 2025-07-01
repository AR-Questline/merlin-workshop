using Awaken.TG.Main.Heroes.HUD.Bars;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Semaphores;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.HUD {
    public abstract class VCHeroHUDBar : ViewComponent<Hero>, ISemaphoreObserver {
        const float Delay = 1f;
        
        [SerializeField] Bar bar;

        FragileSemaphore _hideSemaphore;
        
        protected abstract StatType StatType { get; }
        protected abstract float Percentage { get; }
        protected virtual float PredictionPercentage => 0f;
        public virtual bool ForceShow => !_hideSemaphore.State;

        protected override void OnAttach() {
            _hideSemaphore = new FragileSemaphore(true, this, Delay, true);
            Target.AfterFullyInitialized(Init);
        }

        void Init() {
            Target.ListenTo(Stat.Events.StatChangedBy(StatType), StatChanged, this);
            bar.SetPercentInstant(Percentage);
            bar.SetPrediction(PredictionPercentage);
            Target.GetOrCreateTimeDependent().WithUpdate(UpdateBar);
        }

        void StatChanged(Stat.StatChange change) {
            if (Mathf.Abs(change.value) >= Mathf.Epsilon && Percentage < 1) {
                _hideSemaphore.Set(true);
            }
        }

        void UpdateBar(float deltaTime) {
            bar.SetPercent(Percentage);
            bar.SetPrediction(PredictionPercentage);
            _hideSemaphore.Update();
        }

        protected override void OnDiscard() {
            Target.GetTimeDependent()?.WithoutUpdate(UpdateBar);
        }
    }
}