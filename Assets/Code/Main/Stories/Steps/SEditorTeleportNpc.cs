using System.Linq;
using Awaken.TG.Main.AI.Idle;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("NPC/NPC: Teleport"), NodeSupportsOdin]
    public class SEditorTeleportNpc : EditorStep {
        public LocationReference npcToTeleport;
        public STeleportNpc.TeleportType teleportType = STeleportNpc.TeleportType.ToPosition;
        [HideIf(nameof(IsToLocation))]
        public Vector3 position;
        [ShowIf(nameof(IsToLocation))]
        public LocationReference locRef;

        public bool IsToLocation => teleportType == STeleportNpc.TeleportType.ToLocation;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new STeleportNpc {
                npcToTeleport = npcToTeleport,
                teleportType = teleportType,
                position = position,
                locRef = locRef
            };
        }
    }
    
    public partial class STeleportNpc : StoryStep {
        public LocationReference npcToTeleport;
        public TeleportType teleportType = TeleportType.ToPosition;
        [HideIf(nameof(IsToLocation))]
        public Vector3 position;
        [ShowIf(nameof(IsToLocation))]
        public LocationReference locRef;
        
        public bool IsToLocation => teleportType == TeleportType.ToLocation;
        
        public override StepResult Execute(Story story) {
            Vector3? target = IsToLocation ? locRef.MatchingLocations(story).FirstOrDefault()?.Coords : position;
            if (target == null) {
                Log.Important?.Error($"Invalid location setup in Hero: Teleport in story {story.ID}");
                return StepResult.Immediate;
            }
            
            var npcLocations = npcToTeleport.MatchingLocations(story);
            foreach (var location in npcLocations) {
                var npc = location.TryGetElement<NpcElement>();
                if (npc != null) {
                    NpcTeleporter.Teleport(npc, target.Value, TeleportContext.FromStory);
                }
            }
            
            return StepResult.Immediate;
        }
        
        public enum TeleportType {
            ToPosition = 0,
            ToLocation = 1,
        }
    }
}