using Awaken.Utility;
using System.Linq;
using Awaken.TG.Main.Memories;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    public partial class FlagTracker : BaseSimpleTracker<FlagTrackerAttachment> {
        public override ushort TypeForSerialization => SavedModels.FlagTracker;

        string[] _flags;

        public override void InitFromAttachment(FlagTrackerAttachment spec, bool isRestored) {
            base.InitFromAttachment(spec, isRestored);
            _flags = spec.flags;
        }


        protected override void StartTracking() {
            World.EventSystem.ListenTo(EventSelector.AnySource, StoryFlags.Events.FlagChanged, this, OnFlagChange);
            OnFlagChange(true);
        }

        void OnFlagChange() {
            OnFlagChange(false);
        }
        
        void OnFlagChange(bool fromInitialCheck) {
            GameplayMemory memory = Services.Get<GameplayMemory>();
            bool IsCompleted(string flag) => memory.Context().Get(flag, false);
            
            SetTo(_flags.Count(IsCompleted), fromInitialCheck);
        }
    }
}