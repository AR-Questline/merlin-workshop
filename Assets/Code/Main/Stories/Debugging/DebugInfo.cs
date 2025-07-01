using Awaken.TG.Main.Stories.Execution;

namespace Awaken.TG.Main.Stories.Debugging {
    public class DebugInfo {
        public StepResult stepResult;
        public bool? wereConditionsMet;

        public void Clear() {
            stepResult = null;
            wereConditionsMet = null;
        }

        public void SetResult(StepResult result) {
            stepResult = result;
        }
        
        public void SetConditionsMet(bool conditionsMet) {
            wereConditionsMet = conditionsMet;
        }
    }
}