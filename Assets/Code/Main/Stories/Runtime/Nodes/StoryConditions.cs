using JetBrains.Annotations;

namespace Awaken.TG.Main.Stories.Runtime.Nodes {
    public abstract class StoryConditions {
        public StoryConditionInput[] inputs;
        public StoryCondition[] conditions;
        
        public bool Fulfilled([CanBeNull] Story story, StoryStep step, bool negate) {
            var fulfilled = Fulfilled(story, step);
            return fulfilled ^ negate;
        }
        protected abstract bool Fulfilled([CanBeNull] Story story, StoryStep step);
        
        public abstract byte Type { get; }
        public static StoryConditions Create(byte type) => type switch {
            0 => new StoryAndConditions(),
            1 => new StoryOrConditions(),
            _ => throw new System.Exception()
        };
    }
    
    public struct StoryConditionInput {
        public StoryConditions conditions;
        public bool negate;
        
        public bool Fulfilled([CanBeNull] Story story, StoryStep step) {
            return conditions.Fulfilled(story, step, negate);
        }
        
        public static bool Fulfilled(StoryConditionInput[] inputs, [CanBeNull] Story story, StoryStep step) {
            foreach (var input in inputs) {
                if (input.Fulfilled(story, step) == false) {
                    return false;
                }
            }
            return true;
        }
    }

    public sealed class StoryAndConditions : StoryConditions {
        protected  override bool Fulfilled(Story story, StoryStep step) {
            foreach (var input in inputs) {
                if (input.Fulfilled(story, step) == false) {
                    return false;
                }
            }
            foreach (var condition in conditions) {
                var fulfilled = condition.Fulfilled(story, step);
#if UNITY_EDITOR
                condition.DebugInfo?.SetConditionsMet(fulfilled);
#endif
                if (fulfilled == false) {
                    return false;
                }
            }
            return true;
        }
        
        public override byte Type => 0;
    }
    
    public sealed class StoryOrConditions : StoryConditions {
        protected  override bool Fulfilled(Story story, StoryStep step) {
            foreach (var input in inputs) {
                if (input.Fulfilled(story, step)) {
                    return true;
                }
            }
            foreach (var condition in conditions) {
                var fulfilled = condition.Fulfilled(story, step);
#if UNITY_EDITOR
                condition.DebugInfo?.SetConditionsMet(fulfilled);
#endif
                if (fulfilled) {
                    return true;
                }
            }
            return false;
        }
        
        public override byte Type => 1;
    }
}