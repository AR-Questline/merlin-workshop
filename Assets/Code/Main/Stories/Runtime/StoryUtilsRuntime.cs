using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.Utility.Times;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Stories.Runtime {
    public static class StoryUtilsRuntime {
        public static bool ShouldExecute(Story story, StoryStep step) {
    #if UNITY_EDITOR
            step.DebugInfo?.SetConditionsMet(false);
    #endif
            foreach (var condition in step.conditions) {
                if (condition.Fulfilled(story, step) == false) {
                    return false;
                }
            }

            if (step is IOncePer oncePer) {
                if (!StoryUtilsRuntime.OncePer(story, oncePer)) {
                    return false;
                }
            }
            
            if (step is SChoice choice) {
                if (choice.ShouldBeAvailable(story) == false) {
                    return false;
                }
            }

#if UNITY_EDITOR
            step.DebugInfo?.SetConditionsMet(true);
#endif
            return true;
        }
        
        /// <summary>
        /// Allows steps to behave like it had COncePer condition attached, without creating the condition itself.
        /// Step needs to implement IOncePer and invoke this method at the start of OnExecute 
        /// </summary>
        public static bool OncePer([CanBeNull] Story story, IOncePer oncePer) {
            if (oncePer.SpanFlag == null) {
                return true;
            }
            if (oncePer.Span == TimeSpans.Dialogue && story == null) {
                return true;
            }

            var context = AutoContext(story);
            var memory = oncePer.Span == TimeSpans.Dialogue 
                ? story!.ShortMemory
                : World.Services.Get<GameplayMemory>();

            int day = World.Only<GameRealTime>().WeatherTime.Day;
            int lastTaken = memory.Context(context).Get(oncePer.SpanFlag, 0);

            return GameTimeUtil.HasTimeSpanChanged(day, lastTaken, oncePer.Span);
        }

        public static void StepPerformed(Story story, StoryStep step) {
            {if (step is IOncePer oncePer) {
                OncePerPerformed(story, oncePer);
            }}
            foreach (var condition in step.conditions) {
                foreach (var subCondition in condition.conditions.conditions) {
                    if (subCondition is IOncePer oncePer) {
                        OncePerPerformed(story, oncePer);
                    }
                }
            }
        }
        
        static void OncePerPerformed(Story story, IOncePer oncePer) {
            var context = StoryUtilsRuntime.AutoContext(story);
            var memory = oncePer.Span == TimeSpans.Dialogue ? story.ShortMemory : story.Memory;
            int day = World.Only<GameRealTime>().WeatherTime.Day;
            memory.Context(context).Set(oncePer.SpanFlag, day);
        }
        
        /// <summary>
        /// Create context based on api only.
        /// It cannot create context that respects both Hero and Place.
        /// </summary>
        public static string[] AutoContext([CanBeNull] Story api) {
            var memory = World.Services.Get<GameplayMemory>();
            if (api != null && api.IsSharedBetweenMultipleNPCs && api.OwnerLocation != null) {
                return memory.Contextify(api, api.OwnerLocation);
            } else {
                return memory.Contextify(api);
            }
        }
    }
}