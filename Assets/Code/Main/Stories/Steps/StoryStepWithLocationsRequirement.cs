using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Steps {
    public abstract partial class StoryStepWithLocationRequirement : StoryStep {
        /// <summary>
        /// If there are required locations and they're not existing, the step will be ignored and transferred to an unobservable action
        /// </summary>
        protected abstract LocationReference RequiredLocations { get; }
        
        public override StepResult Execute(Story story) {
            var locationRef = RequiredLocations;
            if (locationRef == null) {
                return StepResult.Immediate;
            }
            
            var execution = GetStepExecution(story);
            if (execution == null) {
                return StepResult.Immediate;
            }
            
            InternalExecute(locationRef, execution, story);
            return StepResult.Immediate;
        }
        
        protected virtual void InternalExecute(LocationReference locationRef, DeferredLocationExecution execution, Story api) {
            if (locationRef.TryGetDistinctiveMatches(out var matches)) {
                var deferredSystem = World.Only<DeferredSystem>();
                foreach (var match in matches) {
                    if (DeferredActionWithLocationMatch.TryExecute(match, execution) == DeferredSystem.Result.Success) {
                        continue;
                    }
                    deferredSystem.RegisterAction(new DeferredActionWithLocationMatch(match, execution));
                }
            } else {
                // if there are no distinctive matches (Self and UnityReference) we execute the action immediately
                foreach (var loc in locationRef.MatchingLocations(api)) {
                    execution.Execute(loc);
                }
            }
        }
        
        protected abstract DeferredLocationExecution GetStepExecution(Story story);
    }
}