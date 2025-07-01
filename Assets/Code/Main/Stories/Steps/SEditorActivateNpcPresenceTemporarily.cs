using System.Collections.Generic;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Utility.RichLabels;
using Awaken.TG.Main.Utility.RichLabels.SO;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("NPC/Presence: Temporary Availability (Activate)")]
    public class SEditorActivateNpcPresenceTemporarily : EditorStep {
        [InfoBox("Disables and caches all currently active presences and activates new ones.\n" +
                 "Works only on this scene and during this story.")]
        [Tooltip("Matching presences will move physically or will be teleported to the destination.")]
        public Travel travelTo = Travel.Teleport;
        public Travel travelBack = Travel.Teleport;

        public RichLabelUsage richLabelUsage = new(RichLabelConfigType.Presence);

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SActivateNpcPresenceTemporarily {
                travelTo = travelTo,
                travelBack = travelBack,
                richLabelUsage = richLabelUsage
            };
        }
    }

    public partial class SActivateNpcPresenceTemporarily : StoryStep {
        public Travel travelTo = Travel.Teleport;
        public Travel travelBack = Travel.Teleport;
        public RichLabelUsage richLabelUsage = new(RichLabelConfigType.Presence);
        
        bool TeleportTo => travelTo == Travel.Teleport;
        bool TeleportBack => travelBack == Travel.Teleport;
        
        public override StepResult Execute(Story story) {
            var temporaryPresences = StoryBasedNpcPresences.GetOrCreate(story);
            foreach (var presence in GetAllPresences()) {
                temporaryPresences.AddPresence(presence, TeleportTo, TeleportBack);
            }
            return StepResult.Immediate;
        }

        StructList<NpcPresence> GetAllPresences() {
            StructList<NpcPresence> presences = new(0);
            var richLabelGuids = richLabelUsage.RichLabelUsageEntries;
            foreach (var location in World.All<Location>()) {
                var presence = location.TryGetElement<NpcPresence>();
                if (presence is null or { Available: true, Attached: true }) {
                    continue;
                }
                if (RichLabelUtilities.IsMatchingRichLabel(presence.RichLabelSet,richLabelGuids)) {
                    presences.Add(presence);
                }
            }
            return presences;
        }
    }
}