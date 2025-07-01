using System.Collections.Generic;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Utils;
using Awaken.Utility.Times;
using Sirenix.OdinInspector;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Delayed/Location: Unobservable action"), NodeSupportsOdin]
    public class SEditorLocationRunUnobserved : EditorStep {
        [LabelWidth(150)]
        public bool requireDistance = true;
        [ShowIf(nameof(requireDistance)), Tooltip("Distance will be calculated from this location.")]
        public LocationReference locationReference;
        [Space]
        public bool waitTime;
        [ShowIf(nameof(waitTime))]
        public ARTimeSpan timeSpan;
        public StoryBookmark targetStory;
        [Tooltip("If you want to start target story with given locations added to it automatically."), LabelWidth(150)]
        public bool useToPassReference;
        [ShowIf(nameof(useToPassReference)), Tooltip("What locations should be added to the target story.")]
        public LocationReference toPassReference;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SLocationRunUnobserved {
                requireDistance = requireDistance,
                locationReference = locationReference,
                waitTime = waitTime,
                timeSpan = timeSpan,
                targetStory = targetStory,
                useToPassReference = useToPassReference,
                toPassReference = toPassReference
            };
        }
    }

    public partial class SLocationRunUnobserved : StoryStep {
        public bool requireDistance = true;
        public LocationReference locationReference;
        public bool waitTime;
        public ARTimeSpan timeSpan;
        public StoryBookmark targetStory;
        public bool useToPassReference;
        public LocationReference toPassReference;
        
        public override StepResult Execute(Story story) {
            List<DeferredCondition> conditions = new();
            if (waitTime) {
                ARDateTime targetTime = World.Only<GameRealTime>().WeatherTime + timeSpan;
                conditions.Add(new DeferredTimeCondition(targetTime));
            }

            if (requireDistance) {
                foreach (var location in locationReference.MatchingLocations(story)) {
                    conditions.Add(new DeferredDistanceCondition(location));
                }
            }

            List<WeakModelRef<Location>> locationsToPass = new(0);
            if (useToPassReference) {
                foreach (var location in toPassReference.MatchingLocations(story)) {
                    conditions.Add(new DeferredLocationExistCondition(location));
                    locationsToPass.Add(location);
                }
            }

            DeferredActionWithBookmark action = new(targetStory, conditions, locationsToPass);
            World.Only<DeferredSystem>().RegisterAction(action);
             
            return StepResult.Immediate;
        }
    }
}