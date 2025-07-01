using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Add To Story"), NodeSupportsOdin]
    public class SEditorLocationAddToStory : EditorStep {
        public LocationReference locations;
        
        [Tooltip("Should the NPC be invulnerable during the story?")]
        public bool invulnerability = true;
        [Tooltip("Should the NPC be involved in Story (play story loop animation and look at their dialogue target)?")]
        public bool involve = true;
        [ShowIf(nameof(involve)), Tooltip("Should the NPC instantly rotate to the Hero?")]
        public bool rotToHero = true;
        [ShowIf(nameof(involve)), Tooltip("Should the NPC be forced to exit interaction?")]
        public bool forceExitInteraction;
        [Tooltip("Should the story wait for NPC involvement? If not, NPC might not be ready for next steps.")]
        public bool waitForInvolvement = true;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SLocationAddToStory {
                locations = locations,
                invulnerability = invulnerability,
                involve = involve,
                rotToHero = rotToHero,
                forceExitInteraction = forceExitInteraction,
                waitForInvolvement = waitForInvolvement
            };
        }
    }

    public partial class SLocationAddToStory : StoryStep {
        public LocationReference locations;
        
        public bool invulnerability = true;
        public bool involve = true;
        public bool rotToHero = true;
        public bool forceExitInteraction;
        public bool waitForInvolvement = true;
        
        public override StepResult Execute(Story story) {
            var stepResult = new StepResult();
            AddLocationsToStory(story, stepResult).Forget();
            return stepResult;
        }
        
        async UniTaskVoid AddLocationsToStory(Story api, StepResult result) {
            List<Location> locs = locations.MatchingLocations(api).ToList();
            if (locs.Count == 0) {
                result.Complete();
                return;
            }
            
            List<UniTask> tasks = new();
            
            foreach (var location in locs) {
                var task = api.SetupLocation(location, invulnerability, involve, rotToHero, forceExitInteraction);
                if (waitForInvolvement) {
                    tasks.Add(task);
                }
            }

            if (!await AsyncUtil.WaitForAll(api, tasks)) {
                return;
            }
            
            result.Complete();
        }
    }
}