using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Stories {
    public partial class HeroCameraDampingOverride : Element<Story> {
        const float DefaultDamping = 4f;
        float _damping;
        
        public new static class Events {
            public static readonly Event<HeroCameraDampingOverride, float> DampingChanged = new(nameof(DampingChanged));
        }
        
        public HeroCameraDampingOverride(float damping) {
            _damping = damping;
        }

        protected override void OnInitialize() {
            SetDamping(_damping);
        }

        protected override void OnDiscard(bool WasDiscardedFromDomainDrop) {
            this.Trigger(Events.DampingChanged, DefaultDamping);
        }

        public void SetDamping(float damping) {
            _damping = damping;
            this.Trigger(Events.DampingChanged, damping);
        }
    }
}