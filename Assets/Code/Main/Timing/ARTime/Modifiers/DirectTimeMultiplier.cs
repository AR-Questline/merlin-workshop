using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Timing.ARTime.Modifiers {
    public partial class DirectTimeMultiplier : Element<TimeDependent>, ITimeModifier {
        public sealed override bool IsNotSaved => true;

        public int Order => 1;
        public string SourceID { get; }
        
        float _multiplier;

        public DirectTimeMultiplier(float multiplier, string sourceID) {
            _multiplier = multiplier;
            SourceID = sourceID;
        }

        public float Modify(float timeScale) {
            return timeScale * _multiplier;
        }

        public void Apply() {
            ParentModel.RefreshTimeScale();
        }

        public void Remove() {
            _multiplier = 1;
            Discard();
        }

        public void Set(float multiplier) {
            _multiplier = multiplier;
            ParentModel.RefreshTimeScale();
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (ParentModel is {HasBeenDiscarded: false}) {
                ParentModel.RefreshTimeScale();
            }

            base.OnDiscard(fromDomainDrop);
        }
    }
}