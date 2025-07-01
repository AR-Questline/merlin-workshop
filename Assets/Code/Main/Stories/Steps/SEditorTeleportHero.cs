using System.Linq;
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
    [Element("Technical/Hero: Teleport"), NodeSupportsOdin]
    public class SEditorTeleportHero : EditorStep {
        public STeleportHero.TeleportType teleportType = STeleportHero.TeleportType.ToPosition;
        [HideIf(nameof(IsToLocation))]
        public Vector3 position;
        [ShowIf(nameof(IsToLocation))]
        public LocationReference locRef;
        
        public bool overrideRotation;
        [ShowIf(nameof(overrideRotation))]
        public Vector3 rotation;

        public bool IsToLocation => teleportType == STeleportHero.TeleportType.ToLocation;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new STeleportHero {
                teleportType = teleportType,
                position = position,
                locRef = locRef,
                overrideRotation = overrideRotation,
                rotation = rotation,
            };
        }
    }

    public partial class STeleportHero : StoryStep {
        public TeleportType teleportType;
        public Vector3 position;
        public LocationReference locRef;
        public bool overrideRotation;
        public Vector3 rotation;
        
        public bool IsToLocation => teleportType == TeleportType.ToLocation;
        
        public override StepResult Execute(Story story) {
            Vector3? target = IsToLocation ? locRef.MatchingLocations(story).FirstOrDefault()?.Coords : position;
            if (target == null) {
                Log.Important?.Error($"Invalid location setup in Hero: Teleport in story {story.ID}");
                return StepResult.Immediate;
            }
            
            StepResult result = new();
            story.Hero.TeleportTo(target.Value, overrideRotation ? Quaternion.Euler(rotation) : null, () => AfterTeleported(result));
            return result;
        }

        void AfterTeleported(StepResult result) {
            result.Complete();
        }
        
        public enum TeleportType {
            ToPosition = 0,
            ToLocation = 1,
        }
    }
}