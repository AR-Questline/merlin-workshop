using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Containers;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Locations.Actions {
    public partial class SearchActionStory : Element<Location>, IRefreshedByAttachment<SearchStoryAttachment> {
        public override ushort TypeForSerialization => SavedModels.SearchActionStory;

        [Saved] bool _triggered;
        
        bool _triggersOnce;
        StoryBookmark _storyBookmark;
        SearchStoryAttachment.SearchTriggersStory _storyMode;
        
        public void InitFromAttachment(SearchStoryAttachment spec, bool isRestored) {
            _storyMode = spec.storyTrigger;
            _storyBookmark = spec.story;
            _triggersOnce = spec.triggerOnce;
        }

        protected override void OnInitialize() {
            base.OnInitialize();
            if (!_storyBookmark.IsValid) {
                Log.Minor?.Error("Invalid story bookmark in SearchActionStory");
                return;
            }
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<ContainerUI>(), this, AttachToContainerUI);
        }

        void AttachToContainerUI(Model container) {
            var containerUI = (ContainerUI) container;
            if (containerUI.ParentModel != ParentModel) {
                return;
            }
            
            containerUI.ListenTo(ContainerUI.ContainerEvents.ContentChanged, ui => {
                if (_storyMode == SearchStoryAttachment.SearchTriggersStory.OnPickup) {
                    // Any item removed
                    if (_triggersOnce && _triggered) {
                        return;
                    }
                    _triggered = true;
                    Story.StartStory(StoryConfig.Interactable(ParentModel, _storyBookmark, typeof(VDialogue)));
                } else if (_storyMode == SearchStoryAttachment.SearchTriggersStory.OnEmptied) {
                    // All items removed
                    if (ui.IsEmpty) {
                        Story.StartStory(StoryConfig.Interactable(ParentModel, _storyBookmark, typeof(VDialogue)));
                    }
                }
            }, this);
        }
    }
}