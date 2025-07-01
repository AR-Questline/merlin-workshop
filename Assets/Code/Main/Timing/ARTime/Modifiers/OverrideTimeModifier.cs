using UnityEngine;

namespace Awaken.TG.Main.Timing.ARTime.Modifiers {
    public partial class OverrideTimeModifier : GradualTimeModifier {
        public sealed override bool IsNotSaved => true;

        float _value;
        
        public override int Order => 100;

        public OverrideTimeModifier(string sourceID, float value, float delay = 0) : base(sourceID, delay) {
            _value = value;
        }
        
        public override float Modify(float timeScale) {
            return Mathf.Lerp(timeScale, _value, Weight);
        }
    }
}