using Awaken.TG.Main.Heroes.HUD;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Heroes.Thievery {
    [UsesPrefab("HUD/Thievery/" + nameof(VTrespassingTracker))]
    public class VTrespassingTracker : VDurationTrackerBase<TrespassingTracker> {
        protected override float InitialDuration => TrespassingTracker.TimeToCrime;
        protected override float MaxDuration => Target.InitialCrimeTimer;
        protected override bool DisableFade => !Target.IsTrespassing;
        protected override bool ShowTimer => Target.IsTimerStarted;
        protected override string InitialText => LocTerms.TrespassingEnteredNotification.Translate();

        protected override void InitListeners() {
            base.InitListeners();
            Target.ListenTo(TrespassingTracker.Events.TrespassingStateChanged, ChangeVisibility, this);
            Target.ListenTo(TrespassingTracker.Events.TimeToCrimeChanged, UpdateTimer, this);
            Target.ListenTo(TrespassingTracker.Events.CrimeStateChanged, UpdateCrimeState, this);
        }

        void UpdateCrimeState() {
            infoLabel.text = Target.IsCrime ? LocTerms.TrespassingAlerted.Translate() : LocTerms.TrespassingEnteredNotification.Translate();
            timerCanvasGroup.alpha = Target.IsTimerStarted ? 1 : 0;
        }
    }
}
