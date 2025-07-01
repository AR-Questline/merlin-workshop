using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations.Actions.Customs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Heroes.HUD {
    [UsesPrefab("HUD/" + nameof(VTrialDurationTracker))]
    public class VTrialDurationTracker : VDurationTrackerBase<ITrialElement> {
        protected override float InitialDuration => Target.TrialRemainingDuration;
        protected override float MaxDuration => Target.TrialDuration;
        protected override bool DisableFade => false;
        protected override bool ShowTimer => true;
        protected override string InitialText => Target.TrialTitle;

        protected override void OnInitialize() {
            base.OnInitialize();
            ChangeVisibility(true);
        }

        protected override void InitListeners() {
            base.InitListeners();
            Target.ListenTo(ITrialElement.Events.TrialTimeUpdate, UpdateTimer, this);
            Target.ListenTo(ITrialElement.Events.TrialEnded, OnTrialEnded, this);
        }

        void OnTrialEnded() {
            ChangeVisibility(false);
            DelayDiscard(FadeDuration).Forget();
        }

        async UniTaskVoid DelayDiscard(float delay) {
            if (!await AsyncUtil.DelayTime(this, delay)) {
                return;
            }
            Discard();
        }
    }
}
