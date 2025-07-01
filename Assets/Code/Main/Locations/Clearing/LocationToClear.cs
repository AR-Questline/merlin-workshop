using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Locations.Clearing {
    public partial class LocationToClear : Element<Location>, IRefreshedByAttachment<LocationToClearAttachment> {
        public override ushort TypeForSerialization => SavedModels.LocationToClear;

        LocationToClearAttachment _spec;
        int _flagsToClear;
        
        public void InitFromAttachment(LocationToClearAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnFullyInitialized() {
            InitAfterOneFrame().Forget();
        }
        
        async UniTaskVoid InitAfterOneFrame() {
            if (await AsyncUtil.DelayFrame(this)) {
                _flagsToClear = _spec.FlagsToClear.Length;
                if (_flagsToClear == 0) {
                    LocationCleared();
                } else {
                    SetupFlags();
                }
            }
        }

        void SetupFlags() {
            for (int i = 0; i < _spec.FlagsToClear.Length; i++) {
                SetupStoryFlag(i, _spec.FlagsToClear[i]);
            }
        }

        void SetupStoryFlag(int index, string flag) {
            var facts = Services.Get<GameplayMemory>().Context();
            if (facts.Get<bool>(flag)) {
                SetFlagAsFulfilled(index);
            } else {
                Reference<IEventListener> listenerReference = new();
                listenerReference.item = World.EventSystem.ListenTo(EventSelector.AnySource, StoryFlags.Events.UniqueFlagChanged(flag), this, f => {
                    if (facts.Get<bool>(flag)) {
                        SetFlagAsFulfilled(index);
                        World.EventSystem.RemoveListener(listenerReference.item);
                    }
                });
            }
        }

        void SetFlagAsFulfilled(int index) {
            if (--_flagsToClear == 0) {
                LocationCleared();
            }
        }

        void LocationCleared() {
            ParentModel.Clear();
            Discard();
        }
    }
}