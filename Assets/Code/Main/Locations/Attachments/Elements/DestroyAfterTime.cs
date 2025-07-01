using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Times;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class DestroyAfterTime : Element<Location>, IRefreshedByAttachment<DestroyAfterTimeAttachment> {
        public override ushort TypeForSerialization => SavedModels.DestroyAfterTime;

        [Saved] ARDateTime _destroyTime;
        DestroyAfterTimeAttachment _spec;
        TimedEvent _destroyTimedEvent;

        public void InitFromAttachment(DestroyAfterTimeAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnInitialize() {
            _destroyTime = World.Only<GameRealTime>().WeatherTime + _spec.DestroyAfter;
            InitTimedEvent();
        }
        
        protected override void OnRestore() {
            if (_spec.DestroyOnRestore) {
                DiscardAfterFrame().Forget();
                return;
            }
            InitTimedEvent();
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            if (_destroyTimedEvent != null) {
                World.Any<GameTimeEvents>()?.RemoveEvent(_destroyTimedEvent);
                _destroyTimedEvent = null;
            }
        }

        void InitTimedEvent() {
            _destroyTimedEvent = new TimedEvent(_destroyTime.Date, DiscardLocation);
            World.Only<GameTimeEvents>().AddEvent(_destroyTimedEvent);
        }

        async UniTaskVoid DiscardAfterFrame() {
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }
            DiscardLocation();
        }

        void DiscardLocation() {
            ParentModel.Discard();
        }
    }
}
