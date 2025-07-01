using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Times;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Make busy"), NodeSupportsOdin]
    public class SEditorLocationMakeBusy : EditorStep {
        public LocationReference locationReference;
        [Tooltip("Story that will be used as dialogue while location is busy.")]
        public StoryBookmark busyStory;
        public ARTimeSpan busyDuration;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SLocationMakeBusy {
                locationReference = locationReference,
                busyStory = busyStory,
                busyDuration = busyDuration
            };
        }
    }

    public partial class SLocationMakeBusy : StoryStepWithLocationRequirement {
        public LocationReference locationReference;
        public StoryBookmark busyStory;
        public ARTimeSpan busyDuration;

        protected override LocationReference RequiredLocations => locationReference;
        
        protected override DeferredLocationExecution GetStepExecution(Story story) {
            if (busyStory.IsValid == false) {
                Log.Important?.Error($"Step {nameof(SEditorLocationMakeBusy)} dont have {nameof(busyStory)} in graph");
                return null;
            }
            return new StepExecution(busyStory, busyDuration);
        }

        public partial class StepExecution : DeferredLocationExecution {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_LocationMakeBusy;

            [Saved] StoryBookmark _busyStory;
            [Saved] ARTimeSpan _busyDuration;
            
            [JsonConstructor, Preserve]
            StepExecution() { }
            
            public StepExecution(StoryBookmark busyStory, ARTimeSpan busyDuration) {
                _busyStory = busyStory;
                _busyDuration = busyDuration;
            }
            
            public override void Execute(Location location) {
                Busy.MakeBusy(location, _busyStory, _busyDuration);
            }
        }
    }
}
