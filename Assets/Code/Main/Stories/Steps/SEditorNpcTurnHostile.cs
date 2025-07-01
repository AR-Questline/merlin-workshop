using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Sirenix.OdinInspector;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("NPC/NPC: Turn Hostile Towards Hero"), NodeSupportsOdin]
    public class SEditorNpcTurnHostile : EditorStep {
        [Header("NPCs that will turn hostile towards target")]
        public LocationReference locations;
        public bool startFight = true;
        public bool allowDeath = true;
        [LabelWidth(170)]
        public bool disableCrimes = true;
        [LabelWidth(170)]
        public bool markDeathAsNonCriminal;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SNpcTurnHostile {
                locations = locations,
                startFight = startFight,
                allowDeath = allowDeath,
                disableCrimes = disableCrimes,
                markDeathAsNonCriminal = markDeathAsNonCriminal
            };
        }
    }

    public partial class SNpcTurnHostile : SNpcTurnHostileBase {
        public LocationReference locations;
        public bool startFight = true;
        public bool allowDeath = true;
        public bool disableCrimes = true;
        public bool markDeathAsNonCriminal;

        protected override LocationReference RequiredLocations => locations;
        protected override DeferredLocationExecution GetStepExecution(Story story) {
            var hostilityData = new HostilityData(allowDeath, disableCrimes, markDeathAsNonCriminal);
            return StepExecution.HostileToHero(startFight, hostilityData);
        }
    }
}