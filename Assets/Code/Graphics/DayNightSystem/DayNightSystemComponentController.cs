using Awaken.TG.Main.Settings.Controllers;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.Utility.Animations;

namespace Awaken.TG.Graphics.DayNightSystem {
    public abstract class DayNightSystemComponentController : StartDependentView<GameRealTime> {
        float _editorTimeOfDay = 0.5f;
        protected float TimeOfDay => GenericTarget != null ? Target.WeatherTime.DayTime : _editorTimeOfDay;

        protected abstract void Init();
        protected abstract void OnUpdate(float deltaTime);

        protected override void OnInitialize() {
            base.OnInitialize();
            Init();
            Target.GetOrCreateTimeDependent().WithUpdate(OnUpdate);
        }

        protected override IBackgroundTask OnDiscard() {
            Target.GetTimeDependent()?.WithoutUpdate(OnUpdate);
            return base.OnDiscard();
        }
        
#if UNITY_EDITOR
        public void EDITOR_Initialize() {
            Init();
        }
        
        public void EDITOR_TimeOfDayChanged(float newTime) {
            _editorTimeOfDay = newTime;
            OnUpdate(0f);
        }
#endif
    }
}