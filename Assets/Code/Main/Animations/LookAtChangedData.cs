using Awaken.TG.Main.Grounds;

namespace Awaken.TG.Main.Animations {
    public readonly struct LookAtChangedData {
        public readonly GroundedPosition groundedPosition;
        public readonly bool lookAtOnlyWithHead;
        
        public LookAtChangedData(GroundedPosition groundedPosition, bool lookAtOnlyWithHead) {
            this.groundedPosition = groundedPosition;
            this.lookAtOnlyWithHead = lookAtOnlyWithHead;
        }
    }
}