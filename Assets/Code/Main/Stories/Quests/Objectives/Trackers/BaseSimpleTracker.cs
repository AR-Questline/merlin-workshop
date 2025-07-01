using Awaken.TG.Main.Locations.Attachments;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    public abstract partial class BaseSimpleTracker : BaseTracker {
        // === State
        protected float Max { get; set; }

        // === Properties
        public override bool IsCompleted => Percent >= 100;
        float Percent => 100f * Current / Max;

        // === Operations
        protected void ChangeBy(float value) {
            SetTo(Current + value);
        }

        protected void SetTo(float value) {
            if (Current != value) {
                Current = Mathf.Clamp(value, 0, Max);
                CheckIfCompleted();
                TriggerChange();
            }
        }

        // === Helpers
        protected override string ConstructDesc() {
            string baseDesc = CustomDisplayPattern.toggled ? CustomDisplayPattern.LocString : DisplayPattern.LocalizedPattern;
            return baseDesc
                .Replace("{cur}", Current.ToString("0"))
                .Replace("{max}", Max.ToString("0"))
                .Replace("{percent}", Percent.ToString("0"));
        }
    }
    
    public abstract partial class BaseSimpleTracker<T> : BaseSimpleTracker, IRefreshedByAttachment<T> where T : BaseSimpleTrackerAttachment {
        // === Constructors
        public virtual void InitFromAttachment(T spec, bool isRestored) {
            if (Current < spec.initialProgress) {
                Current = spec.initialProgress;
            }
            DisplayPattern = spec.DisplayPattern ?? TrackerDisplayPattern.None;
            CustomDisplayPattern = spec.customDisplayPattern;
            StartDisplayPatternInNewLine = spec.startDisplayPatternInNewLine;
            FlagToMap = spec.flagToMap;
            Max = spec.maxProgress;
            WhenOtherTrackersPresent = spec.whenOtherTrackersPresent;
            TaskType = spec.taskType;
            RequiredObjectiveState = spec.requiredObjectiveState;
            TargetState = spec.targetState;
            ObjectiveChangesOnFulfilled = spec.objectiveChangesOnFulfilled;
            ObjectiveChangesOnLoseFulfillment = spec.objectiveChangesOnLoseFulfillment;
        }
    }
}