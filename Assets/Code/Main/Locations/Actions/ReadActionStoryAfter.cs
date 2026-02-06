using Awaken.Utility;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Locations.Actions {
    public partial class ReadActionStoryAfter : Element<Location>, IRefreshedByAttachment<ReadStoryAfterAttachment> {
        public override ushort TypeForSerialization => SavedModels.ReadActionStoryAfter;

        ReadStoryAfterAttachment _spec;

        public void InitFromAttachment(ReadStoryAfterAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnFullyInitialized() {
            base.OnFullyInitialized();
            if (!ParentModel.TryGetElement<ReadAction>(out var readAction)) {
                Discard();
                return;
            }
            readAction.ListenTo(ReadAction.Events.StoryEnded, StartStory, this);
        }

        void StartStory(Story _) {
            Story.StartStory(StoryConfig.Location(ParentModel, _spec.bookmark, typeof(VDialogue)));
        }
    }
}