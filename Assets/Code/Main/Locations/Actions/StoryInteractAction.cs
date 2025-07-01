using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Locations.Actions {
    /// <summary>
    /// Action for running story and displaying interaction name from SStoryStartChoice on the UI
    /// </summary>
    public partial class StoryInteractAction : AbstractLocationAction, ILocationNameModifier, IRefreshedByAttachment<StoryInteractAttachment> {
        public override ushort TypeForSerialization => SavedModels.StoryInteractAction;

        StoryBookmark StoryBookmark { get; set; }
        bool ShowInteractionInfoIfBlocked { get; set; }

        bool _graphCreated;
        StoryGraphRuntime _graph;
        SStoryStartChoice _startChoice;
        
        public int ModificationOrder => 10;
        public override InfoFrame ActionFrame => AreConditionsFulfilled() ? 
            new InfoFrame(ActionDescription, true) : 
            new InfoFrame(LocTerms.Blocked.Translate(), false);

        string ActionDescription { get {
            EnsureGraph();
            return _startChoice?.text ?? string.Empty;
        }}

        public void InitFromAttachment(StoryInteractAttachment spec, bool isRestored) {
            StoryBookmark = spec.storyBookmark;
            ShowInteractionInfoIfBlocked = spec.showInteractionInfoIfBlocked;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (_graphCreated) {
                _graph.Dispose();
                _graphCreated = false;
            }
        }

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            Story.StartStory(StoryConfig.Interactable(interactable, StoryBookmark, null));
        }

        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            if (AreConditionsFulfilled()) {
                return ActionAvailability.Available;
            }
            
            return ShowInteractionInfoIfBlocked ? ActionAvailability.Available : ActionAvailability.Disabled;
        }

        bool AreConditionsFulfilled() {
            EnsureGraph();
            if (_startChoice == null) {
                return false;
            }
            return StoryConditionInput.Fulfilled(_startChoice.conditions, null, _startChoice);
        }

        public string ModifyName(string original) {
            if (!AreConditionsFulfilled()) {
                return ActionDescription;
            }
            
            return original;
        }

        void EnsureGraph() {
            if (_graphCreated) {
                return;
            }
            _graphCreated = true;
            if (StoryBookmark.IsValid == false) {
                return;
            }
            var graph = StoryGraphRuntime.Get(StoryBookmark.GUID);
            if (!graph.HasValue) {
                Log.Important?.Error($"Broken story in StoryInteractAction {this}: {StoryBookmark.GUID}.");
                return;
            }
            _graph = graph.Value;
            if (_graph.startNode != null && _graph.startNode.choices.Length > 0) {
                _startChoice = _graph.startNode.choices[0];
            } else {
                Log.Important?.Error($"SStoryStartChoice step is missing in story {_graph.guid}.");
            }
        }
    }
}