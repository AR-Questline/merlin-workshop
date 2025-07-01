using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    
    public partial class TemporarilyInactive : Element<Location> {
        public override ushort TypeForSerialization => SavedModels.TemporarilyInactive;

        readonly float _inactivityTime;
        bool _restored;
        
        [UnityEngine.Scripting.Preserve]
        TemporarilyInactive() {}

        public TemporarilyInactive(float inactivityTime) {
            _inactivityTime = inactivityTime;
        }

        protected override void OnInitialize() {
            ParentModel.SetInteractability(LocationInteractability.Inactive);
            ReactivateDelay().Forget();
        }

        protected override void OnRestore() {
            _restored = true;
        }
        
        protected override void OnFullyInitialized() {
            if (_restored) {
                Reactivate();
            }
        }

        async UniTaskVoid ReactivateDelay() {
            if (await AsyncUtil.DelayTime(this, _inactivityTime)) {
                Reactivate();
            }
        }

        void Reactivate() {
            ParentModel.SetInteractability(LocationInteractability.Active);
            Discard();
        }
    }
}
