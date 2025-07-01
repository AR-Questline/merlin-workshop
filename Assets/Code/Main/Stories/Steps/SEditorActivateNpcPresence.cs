using System.Linq;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("NPC/Presence: Change Availability (OLD)"), NodeSupportsOdin]
    public class SEditorActivateNpcPresence : EditorStep {
        [InfoBox("DON'T USE THIS STEP, USE RICH LABELS", InfoMessageType.Error)]
        public LocationReference presences;

        [Tooltip("Changes behaviour, all matching presences will be activated or deactivated."), HideLabel]
        public Mode mode = Mode.SetAvailable;

        [ShowIf(nameof(Availability)), Tooltip("Matching presences will move physically or will be teleported to the destination.")]
        public Travel travel = Travel.Teleport;

        bool Availability => mode == Mode.SetAvailable;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SActivateNpcPresence {
                presences = presences,
                mode = mode,
                travel = travel,
            };
        }
    }

    public partial class SActivateNpcPresence : StoryStep {
        public LocationReference presences;
        public Mode mode = Mode.SetAvailable;
        public Travel travel = Travel.Teleport;
        
        bool Availability => mode == Mode.SetAvailable;
        bool Teleport => travel == Travel.Teleport;
        
        public override StepResult Execute(Story story) {
            Log.Important?.Error($"SActivateNpcPresence is deprecated, use Rich Labels instead. {story.Guid}");
            
            var locationRef = presences;
            if (locationRef == null) {
                return StepResult.Immediate;
            }

            var execution = new SActivateNpcPresenceViaRichLabels.StepExecution(Availability, Teleport);

            var matchingPresenceSources = PresenceCache.Get.GetMatchingPresenceData(presences);
            if (matchingPresenceSources.Any()) {
                var deferredSystem = World.Only<DeferredSystem>();
                foreach (var presenceSource in matchingPresenceSources) {
                    LocationReference.MatchByAllTags match = new (presenceSource.tags);
                    if (DeferredActionWithLocationMatch.TryExecute(match, execution) == DeferredSystem.Result.Success) {
                        continue;
                    }
                    deferredSystem.RegisterAction(new DeferredActionWithLocationMatch(match, execution));
                }

                return StepResult.Immediate;
            } else {
#if !UNITY_EDITOR
                Debug.LogException(new System.Exception($"No presence found by {nameof(SActivateNpcPresence)} in story: {story.Guid}"));
#endif
            }

#if UNITY_EDITOR
            // This is the fallback for newly created presences, which are not yet baked into the cache.
            // This is part of the old way of activating presences, which is now deprecated.
            if (locationRef.TryGetDistinctiveMatches(out var matches)) {
                var deferredSystem = World.Only<DeferredSystem>();
                foreach (var match in matches) {
                    if (DeferredActionWithLocationMatch.TryExecute(match, execution) == DeferredSystem.Result.Success) {
                        continue;
                    }

                    deferredSystem.RegisterAction(new DeferredActionWithLocationMatch(match, execution));
                }

                return StepResult.Immediate;
            }
#endif
            // if there are no distinctive matches (Tries to match presence with anything but tag) we execute the action immediately
            // Since only tags make sense in this context, this is the fallback for cases when we have no tags.
            var focusedLocation = story?.FocusedLocation;
            if (focusedLocation) {
                execution.Execute(focusedLocation);
            }
            return StepResult.Immediate;
        }
    }
}