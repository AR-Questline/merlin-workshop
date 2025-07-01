using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Sirenix.OdinInspector;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("NPC/NPC: Turn Hostile Towards Other NPC"), NodeSupportsOdin, LabelWidth(130)]
    public class SEditorNpcTurnHostileToNpc : EditorStep {
        [Header("NPCs that will turn hostile towards target")]
        public LocationReference locations;
        public bool startFight = true;
        public bool allowDeath = true;
        public bool disableCrimes = true;
        [LabelWidth(180)]
        public bool markDeathAsNonCriminal;

        [Header("NPCs that will be targeted")]
        public LocationReference locationsToTarget;
        public bool targetHeroAsWell;
        public bool allowTargetsDeath = true;
        public bool disableTargetsCrimes = true;
        [LabelWidth(180)]
        public bool markTargetsDeathAsNonCriminal;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SNpcTurnHostileToNpc {
                locations = locations,
                startFight = startFight,
                allowDeath = allowDeath,
                disableCrimes = disableCrimes,
                markDeathAsNonCriminal = markDeathAsNonCriminal,
                locationsToTarget = locationsToTarget,
                targetHeroAsWell = targetHeroAsWell,
                allowTargetsDeath = allowTargetsDeath,
                disableTargetsCrimes = disableTargetsCrimes,
                markTargetsDeathAsNonCriminal = markTargetsDeathAsNonCriminal
            };
        }
    }

    public partial class SNpcTurnHostileToNpc : SNpcTurnHostileBase {
        public LocationReference locations;
        public LocationReference locationsToTarget;
        
        public bool startFight;
        public bool allowDeath;
        public bool disableCrimes;
        public bool markDeathAsNonCriminal;

        public bool targetHeroAsWell;
        public bool allowTargetsDeath;
        public bool disableTargetsCrimes;
        public bool markTargetsDeathAsNonCriminal;

        protected override LocationReference RequiredLocations => locations;
        protected override DeferredLocationExecution GetStepExecution(Story story) {
            var hostilityData = new HostilityData(allowDeath, disableCrimes, markDeathAsNonCriminal);
            var targetsHostilityData = new HostilityData(allowTargetsDeath, disableTargetsCrimes, markTargetsDeathAsNonCriminal);
            return new StepExecution(startFight, hostilityData, locationsToTarget, targetHeroAsWell, targetsHostilityData);
        }
    }
}