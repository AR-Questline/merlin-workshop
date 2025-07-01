using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Timing.ARTime.Modifiers;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Modifiers {
    public partial class SlowDownTime : DurationProxy<Hero> {
        public sealed override bool IsNotSaved => true;

        public override IModel TimeModel => ParentModel;
        string GlobalSourceID => ParentModel.ID + ":GlobalTimeModifier";

        float _timeElapsed;
        AnimationCurve _slowDownCurve;
        MultiplyTimeModifier _globalTimeModifier;

        public SlowDownTime(IDuration duration, AnimationCurve slowDownCurve) : base(duration) {
            _slowDownCurve = slowDownCurve;
        }
        
        protected override void OnInitialize() {
            _globalTimeModifier = new MultiplyTimeModifier(GlobalSourceID, _slowDownCurve.Evaluate(_timeElapsed));
            World.Only<GlobalTime>().AddTimeModifier(_globalTimeModifier);
            ParentModel.GetOrCreateTimeDependent().WithUpdate(ProcessUpdate);
        }

        void ProcessUpdate(float deltaTime) {
            _timeElapsed += deltaTime;
            _globalTimeModifier.ChangeScale(_slowDownCurve.Evaluate(_timeElapsed));
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            if (!fromDomainDrop) {
                ParentModel.GetTimeDependent()?.WithoutUpdate(ProcessUpdate);
                World.Only<GlobalTime>().RemoveTimeModifiersFor(GlobalSourceID);
                ParentModel.FoV.EndBowSlowTimeZoom();
            }
            _globalTimeModifier = null;
        }

        void Renew(IDuration duration, AnimationCurve slowDownCurve) {
            Duration.Renew(duration);
            _timeElapsed = 0;
            _slowDownCurve = slowDownCurve;
        }
        
        // === Public API
        public static void SlowTime(IDuration duration, AnimationCurve slowDownCurve, float fovChange = 1f) {
            Hero h = Hero.Current;
            h.FoV.ApplySlowTimeZoom(fovChange);
            SlowDownTime currentSlowDownTime = h.TryGetElement<SlowDownTime>();
            if (currentSlowDownTime != null) {
                currentSlowDownTime.Renew(duration, slowDownCurve);
            } else {
                h.AddElement(new SlowDownTime(duration, slowDownCurve));
            }
        }
    }
}