using System;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Time: Block|Enable")]
    public class SEditorTimeBlock : EditorStep {
        [Tooltip("Used as safe mechanism to identify TimeBlockers by their ids, so that if someone forgets to unblock time, we can find who blocked it")]
        public string id;
        public TimeType timeType = TimeType.Weather;
        public bool enable;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new STimeBlock {
                id = id,
                timeType = timeType,
                enable = enable
            };
        }
    }
    
    public partial class STimeBlock : StoryStep {
        public string id;
        public TimeType timeType = TimeType.Weather;
        public bool enable;
        
        public override StepResult Execute(Story story) {
            if (string.IsNullOrWhiteSpace(id)) {
                throw new ArgumentException($"You need to specify ID of the Time Block");
            }
            
            if (enable) {
                World.All<TimeBlocker>().FirstOrDefault(b => b.SourceID == id)?.Discard();
            } else {
                if (World.All<TimeBlocker>().All(b => b.SourceID != id)) {
                    World.Add(new TimeBlocker(id, timeType));
                }
            }
            return StepResult.Immediate;
        }
    }
}