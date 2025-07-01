using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps;
using Awaken.Utility.Enums;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Debugging {
    public class DebugResult : RichEnum {
        public Color Color { get; }
        public string DisplayName { get; }

        public static readonly DebugResult
            // steps
            Waiting = new DebugResult(nameof(Waiting), "Waiting", Color.grey),
            ExecutedSuccess = new DebugResult(nameof(ExecutedSuccess), "Success", Color.green),
            ExecutedFailure = new DebugResult(nameof(ExecutedFailure), "Failure", Color.red),
            Processing = new DebugResult(nameof(Processing), "Processing", Color.yellow),
            // conditions
            ConditionTrue = new DebugResult(nameof(ConditionTrue), "True", Color.green),
            ConditionFalse = new DebugResult(nameof(ConditionFalse), "False", Color.red);

        protected DebugResult(string enumName, string displayName, Color color) : base(enumName) {
            DisplayName = displayName;
            Color = color;
        }

        public static DebugResult FindResult(NodeElement element) {
            DebugInfo info = element.DebugInfo;
            if (element is EditorStep) {
                bool isDone = info.stepResult?.IsDone ?? false;
                bool conditionsNotMet = !info.wereConditionsMet ?? false;

                if (isDone) {
                    return ExecutedSuccess;
                } else if (conditionsNotMet) {
                    return ExecutedFailure;
                } else if (info.stepResult != null) {
                    return Processing;
                } else {
                    return Waiting;
                }
            } else if (element is EditorCondition) {
                if (info.wereConditionsMet == null) {
                    return Waiting;
                } else if (info.wereConditionsMet.Value) {
                    return ConditionTrue;
                } else {
                    return ConditionFalse;
                }
            } else {
                return Processing;
            }
        }
    }
}