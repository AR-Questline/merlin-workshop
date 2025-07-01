using Awaken.Utility;
using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Utility.RichLabels;
using Awaken.TG.Main.Utility.RichLabels.SO;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("NPC/Presence: Change Availability")]
    public class SEditorActivateNpcPresenceViaRichLabels : EditorStep {
        [Tooltip("Changes behaviour, all matching presences will be activated or deactivated.")]
        public Mode mode = Mode.SetAvailable;

        [ShowIf(nameof(Availability)), Tooltip("Matching presences will move physically or will be teleported to the destination.")]
        public Travel travel = Travel.Teleport;
        
        public RichLabelUsage richLabelUsage = new(RichLabelConfigType.Presence);
        bool Availability => mode == Mode.SetAvailable;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SActivateNpcPresenceViaRichLabels {
                mode = mode,
                travel = travel,
                richLabelUsage = richLabelUsage
            };
        }
    }

    public partial class SActivateNpcPresenceViaRichLabels : StoryStep {
        public Mode mode = Mode.SetAvailable;
        public Travel travel = Travel.Teleport;
        public RichLabelUsage richLabelUsage = new(RichLabelConfigType.Presence);
        
        bool Availability => mode == Mode.SetAvailable;
        bool Teleport => travel == Travel.Teleport;
        
        public override StepResult Execute(Story story) {
            var execution = new StepExecution(Availability, Teleport);
            var richLabelGuids = richLabelUsage.RichLabelUsageEntries;

            World.Services.Get<PresenceTrackerService>().UpdatePresence(new (richLabelUsage, Availability));
            
            var matchingPresences = PresenceCache.Get.GetMatchingPresenceData(richLabelGuids);
#if !UNITY_EDITOR
            if (matchingPresences.Length == 0) {
                Debug.LogException(new Exception($"No presence {richLabelUsage} found by {nameof(SActivateNpcPresenceViaRichLabels)} in story: {story.Guid}"));
            }
#endif
            
            var deferredSystem = World.Only<DeferredSystem>();
            foreach (var presence in matchingPresences) {
                PresenceData presenceData = new (presence);
                if (DeferredActionWithPresenceMatch.TryExecute(presenceData, execution) == DeferredSystem.Result.Success) {
                    continue;
                }
                
                deferredSystem.RegisterAction(new DeferredActionWithPresenceMatch(presenceData, execution));
            }

#if UNITY_EDITOR
            // This is the fallback for newly created presences, which are not yet baked into the cache.
            // This is part of the old way of activating presences, which is now deprecated.
            if (LocationReference.TryGetDistinctiveMatches(richLabelGuids, out var matches)) {
                foreach (var match in matches) {
                    if (DeferredActionWithLocationMatch.TryExecute(match, execution) == DeferredSystem.Result.Success) {
                        continue;
                    }

                    deferredSystem.RegisterAction(new DeferredActionWithLocationMatch(match, execution));
                }
            }
#endif
            return StepResult.Immediate;
        }
        
        public partial class StepExecution : DeferredLocationExecution {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_ActivateNpcPresenceViaRichLabels;

            [Saved] bool _availability;
            [Saved] bool _teleport;

            [JsonConstructor, Preserve]
            StepExecution() { }
            
            public StepExecution(bool availability, bool teleport) {
                _availability = availability;
                _teleport = teleport;
            }

            public override void Execute(Location location) {
                location.TryGetElement<NpcPresence>()?.SetManualAvailability(_availability, _teleport);
            }
        }
    }

    [Serializable]
    public partial class PresenceData {
        public ushort TypeForSerialization => SavedTypes.PresenceData;

        [Saved] public SceneReference sceneRef;
        [Saved] public RichLabelSet richLabelSet;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        PresenceData() {}

        public PresenceData(PresenceSource presenceSource) {
            this.sceneRef = presenceSource.motherScene;
            this.richLabelSet = presenceSource.richLabelSet;
        }
    }
    
    public enum Mode {
        SetAvailable,
        SetUnavailable,
    }

    public enum Travel {
        Move,
        Teleport
    }
}