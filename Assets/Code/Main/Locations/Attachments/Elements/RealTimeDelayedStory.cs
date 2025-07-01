using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Times;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class RealTimeDelayedStory : Element<Location>, IRefreshedByAttachment<RealTimeDelayedStoryAttachment> {
        public override ushort TypeForSerialization => SavedModels.RealTimeDelayedStory;

        [Saved] ARDateTime _cachedTime;
        RealTimeDelayedStoryAttachment _spec;
        
        public void InitFromAttachment(RealTimeDelayedStoryAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnInitialize() {
            var currentSystemTime = System.DateTime.UtcNow;
            _cachedTime = new ARDateTime(currentSystemTime);
        }

        protected override void OnRestore() {
            var currentSystemTime = System.DateTime.UtcNow;
            if (currentSystemTime < _cachedTime + _spec.ActionDelay) {
                if (_spec.ResetTimerOnFailedActivation) {
                    _cachedTime = new ARDateTime(currentSystemTime);
                }
                return;
            }
            TriggerAction();
        }

        void TriggerAction() {
            Story.StartStory(StoryConfig.Location(ParentModel, _spec.Story, typeof(VDialogue)));
            Discard();
        }
    }
}
