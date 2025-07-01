using System;
using System.Collections.Generic;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Execution;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Stories.Steps {
    [Serializable]
    public abstract partial class StoryStepWithLocationRequirementAllowingWait : StoryStepWithLocationRequirement {
        public sealed override StepResult Execute(Story story) {
            var locationRef = RequiredLocations;
            if (locationRef == null) {
                return StepResult.Immediate;
            }

            var execution = GetStepExecution(story);
            if (execution == null) {
                return StepResult.Immediate;
            }
            if (execution is not DeferredLocationExecutionAllowingWait executionAllowingWait) {
                throw new InvalidOperationException($"Expected {nameof(DeferredLocationExecutionAllowingWait)} but got {execution.GetType()}");
            }

            if (executionAllowingWait.ShouldPerformAndWait) {
                var stepResult = new StepResult();
                ExecuteAndWait(locationRef, story, stepResult, executionAllowingWait).Forget();
                return stepResult;
            } else {
                InternalExecute(locationRef, execution, story);
                return StepResult.Immediate;
            }
        }

        public async UniTaskVoid ExecuteAndWait(LocationReference locationRef, Story api, StepResult stepResult, DeferredLocationExecutionAllowingWait execution) {
            var tasks = new List<UniTask>();
            foreach (var loc in locationRef.MatchingLocations(api)) {
                tasks.Add(execution.ExecuteAndWait(loc, api));
            }

            if (!await AsyncUtil.WaitForAll(api, tasks)) {
                return;
            }
            stepResult.Complete();
        }
    }
}