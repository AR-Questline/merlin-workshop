using System;
using System.Collections.Generic;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.Utility.Times;

namespace Awaken.TG.Main.Stories.Steps {
    [Serializable]
    public abstract partial class StoryStepWithTimeRequirement : StoryStep {
        public ARTimeSpan timeSpan;
        
        public sealed override StepResult Execute(Story story) {
            if (timeSpan.TotalSeconds == 0) {
                GetStepExecution(story).Execute();
                return StepResult.Immediate;
            }

            ARDateTime targetTime = World.Only<GameRealTime>().WeatherTime + timeSpan;
            List<DeferredCondition> conditions = new() { new DeferredTimeCondition(targetTime) };
            World.Only<DeferredSystem>().RegisterAction(new DeferredActionWithStoryStep(GetStepExecution(story), conditions));
            return StepResult.Immediate;
        }

        public abstract DeferredStepExecution GetStepExecution(Story story);
    }
}