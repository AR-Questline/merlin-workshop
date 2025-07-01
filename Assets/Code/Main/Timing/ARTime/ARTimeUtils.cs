using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Timing.ARTime.Modifiers;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Pathfinding;
using UnityEngine;

namespace Awaken.TG.Main.Timing.ARTime {
    public static class ARTimeUtils {
        // === Time Dependents
        
        public static TimeDependent GetOrCreateTimeDependent(this IModel model) {
            return model.GetTimeDependent() ?? model.CreateTimeDependent();
        }
        
        public static TimeDependent GetOrCreateTimeDependent(this Hero hero) {
            return hero.GetTimeDependent() ?? hero.CreateTimeDependent();
        }
        
        public static TimeDependent GetOrCreateTimeDependent(this NpcElement npc) {
            return npc.GetTimeDependent() ?? npc.CreateTimeDependent();
        }
        
        public static TimeDependent GetTimeDependent(this IModel model) {
            if (model is Element<Location> element) {
                return element.ParentModel?.TryGetElement<TimeDependent>();
            }
            return model.TryGetElement<TimeDependent>();
        }

        public static TimeDependent GetTimeDependent(this Hero hero) {
            return hero.TimeDependent;
        }
        
        public static TimeDependent GetTimeDependent(this NpcElement npc) {
            return npc.TimeDependent;
        }

        public static TimeDependent CreateTimeDependent(this IModel model) {
            var dependent = new TimeDependent();
            if (model is Element<Location> element) {
                element.ParentModel.AddElement(dependent);
            } else {
                model.AddElement(dependent);
            }
            return dependent;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static bool IsTimeDependent(this IModel model) {
            if (model is Element<Location> element) {
                return element.ParentModel.HasElement<TimeDependent>();
            }
            return model.HasElement<TimeDependent>();
        }

        // === Time Modifiers
        
        public static void AddTimeModifier(this IModel model, ITimeModifier modifier) {
            model.GetOrCreateTimeDependent()?.AddTimeModifier(modifier);
        }
        public static void RemoveTimeModifiersFor(this IModel model, string sourceID) {
            model.GetTimeDependent()?.RemoveTimeModifiersFor(sourceID);
        }
        
        // === Time Properties
        
        public static float GetTimeScaleModifier(this IModel model) {
            return model.GetTimeDependent()?.TimeScaleModifier ?? 1f;
        }
        
        public static float GetTimeScaleModifier(this IView view) {
            return view.GenericTarget.GetTimeScaleModifier();
        }
        
        public static float GetTimeScale(this IModel model) {
            return model.GetTimeScaleModifier() * Time.timeScale;
        }

        public static float GetTimeScale(this IView view) {
            return view.GenericTarget.GetTimeScale();
        }

        [UnityEngine.Scripting.Preserve]
        public static float GetTimeScale(this ViewComponent viewComponent) {
            return viewComponent.GenericTarget.GetTimeScale();
        }

        public static float GetDeltaTime(this IModel model) {
            return model.GetTimeDependent()?.DeltaTime ?? Time.deltaTime;
        }
        
        public static float GetDeltaTime(this NpcElement npc) {
            return npc.TimeDependent?.DeltaTime ?? Time.deltaTime;
        }

        public static float GetDeltaTime(this Hero hero) {
            return hero.TimeDependent?.DeltaTime ?? Time.deltaTime;
        }
        
        public static float GetDeltaTime(this IView view) {
            return view.GenericTarget.GetDeltaTime();
        }
        
        public static float GetDeltaTime(this ViewComponent viewComponent) {
            return viewComponent.GenericTarget.GetDeltaTime();
        }

        public static float GetFixedDeltaTime(this IModel model) {
            return model.GetTimeDependent()?.FixedDeltaTime ?? Time.fixedDeltaTime;
        }
        
        public static float GetFixedDeltaTime(this NpcElement npc) {
            return npc.TimeDependent?.FixedDeltaTime ?? Time.fixedDeltaTime;
        }
        
        public static float GetFixedDeltaTime(this IView view) {
            return view.GenericTarget.GetFixedDeltaTime();
        }
        
        [UnityEngine.Scripting.Preserve]
        public static float GetFixedDeltaTime(this ViewComponent viewComponent) {
            return viewComponent.GenericTarget.GetFixedDeltaTime();
        }
        
        // === Bolt Utils
        [UnityEngine.Scripting.Preserve]
        public static float DeltaTimeOf(GameObject go) => go?.GetComponentInParent<IView>()?.GetDeltaTime() ?? Time.deltaTime;
        
        [UnityEngine.Scripting.Preserve] 
        public static float FixedDeltaTimeOf(GameObject go) => go?.GetComponentInParent<IView>()?.GetFixedDeltaTime() ?? Time.fixedDeltaTime;
        
        [UnityEngine.Scripting.Preserve] 
        public static float TimeScaleOf(GameObject go) => go?.GetComponentInParent<IView>()?.GetTimeScale() ?? 1;
        
        // === Time Components

        public static void SetAnimatorSpeed(Animator animator, float speed) {
            animator.speed = animator.GetComponentInParent<IView>().GetTimeScaleModifier() * speed;
        }
        
        [UnityEngine.Scripting.Preserve] 
        public static void SetAgentSpeed(RichAI agent, float speed) {
            agent.maxSpeed = agent.GetComponentInParent<IView>().GetTimeScaleModifier() * speed;
        }
    }
}