using UnityEngine;

namespace Awaken.TG.Main.Timing.ARTime.Modifiers {
    public partial class MultiplyTimeModifier : GradualTimeModifier {
        public sealed override bool IsNotSaved => true;

        float _scale;
        
        public override int Order => 1;

        public MultiplyTimeModifier(string sourceID, float scale, float delay = 0) : base(sourceID, delay) {
            _scale = scale;
        }

        public void ChangeScale(float scale) {
            _scale = scale;
            ParentModel.RefreshTimeScale();
        }
        
        public override float Modify(float timeScale) {
            return timeScale * Mathf.Lerp(1, _scale, Weight);
        }
    }
}