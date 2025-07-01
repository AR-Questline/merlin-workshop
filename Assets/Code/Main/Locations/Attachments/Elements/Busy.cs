using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Times;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class Busy : Element<Location>, IWithDuration {
        public override ushort TypeForSerialization => SavedModels.Busy;

        [Saved] StoryBookmark _busyStory;
        WeakModelRef<DialogueAction> _dialogueAction;

        public IModel TimeModel => this;

        [JsonConstructor, UnityEngine.Scripting.Preserve] Busy() {}
        public Busy(StoryBookmark busy, ARTimeSpan busyTime) {
            _busyStory = busy;
            AddElement(new GameTimeDuration(busyTime));
        }

        public static Busy MakeBusy(Location location, StoryBookmark busyStory, ARTimeSpan busyTime) {
            Busy busy = location.TryGetElement<Busy>();
            if (busy) {
                busy.Element<GameTimeDuration>().Prolong(busyTime);
            } else {
                busy = location.AddElement(new Busy(busyStory, busyTime));
            }
            return busy;
        }

        protected override void OnFullyInitialized() {
            ParentModel.AfterFullyInitialized(() => {
                SetupBusyStory();
                SetupListeners();
            }, this);
        }

        protected override void OnDiscard(bool _) {
            if (_dialogueAction.TryGet(out var dialogueAction) && !dialogueAction.HasBeenDiscarded) {
                dialogueAction.RemoveStoryOverride(_busyStory);
                _dialogueAction = default;
            }
        }

        void SetupListeners() {
            Element<GameTimeDuration>().ListenTo(Events.AfterDiscarded, _ => Discard(), this);
            ParentModel.ListenTo(Events.AfterElementsCollectionModified, BusyLocationElementsChanged, this);
        }

        void BusyLocationElementsChanged(Model _) {
            if (_dialogueAction.Get() == null) {
                SetupBusyStory();
            }
        }

        void SetupBusyStory() {
            DialogueAction dialogueAction;
            if (!(dialogueAction = ParentModel.TryGetElement<DialogueAction>())) {
                dialogueAction = ParentModel.AddElement<DialogueAction>();
            }
            _dialogueAction = dialogueAction;
            dialogueAction.PushStoryOverride(_busyStory);
        }
    }
}
