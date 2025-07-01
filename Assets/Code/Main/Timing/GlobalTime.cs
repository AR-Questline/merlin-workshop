using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Timing {
    /// <summary>
    /// Model responsible for changing Time.timeScale. It should be done nowhere else. <br/>
    /// You can modify global time scale only by adding/removing ITimeModifiers. <br/>
    /// see GameRealTime.PauseTime and QuickUseWheel._timeMultiplier
    /// </summary>
    public partial class GlobalTime : Model {
        public override ushort TypeForSerialization => SavedModels.GlobalTime;

        public override Domain DefaultDomain => Domain.Gameplay;

        public const float FixedTimeStep = 0.02f;
        const float MinFixedTimeStep = FixedTimeStep / 10f;

        // ReSharper disable once InconsistentNaming
        float DEBUG_TimeScale {
            [UnityEngine.Scripting.Preserve] get => Time.timeScale;
            set => UpdateTimeScale(Time.timeScale, value);
        }
        
        protected override void OnInitialize() {
            var timeDependent = new TimeDependent()
                .WithTimeScaleChanged(UpdateTimeScale)
                .WithIgnoreGameRealTimeModifiers(true);
            AddElement(timeDependent);
        }

        void UpdateTimeScale(float from, float to) {
            Time.timeScale = to;
            if (to != 0f) {
                // Updating only when not pausing to avoid weird physics behaviours
                Time.fixedDeltaTime = math.max(MinFixedTimeStep, FixedTimeStep * to);
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            Time.fixedDeltaTime = FixedTimeStep;
            Time.timeScale = 1;
        }

        public void DEBUG_SetTimeScale(float timeScale) {
            DEBUG_TimeScale = timeScale;
        }
    }
}