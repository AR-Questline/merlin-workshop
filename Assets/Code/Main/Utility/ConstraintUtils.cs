using UnityEngine.Animations;

namespace Awaken.TG.Main.Utility {
    public static class ConstraintUtils {
        [UnityEngine.Scripting.Preserve]
        public static void SetOrAdd(this PositionConstraint constraint, ConstraintSource constraintSource) {
            if (constraint.sourceCount > 0) {
                constraint.SetSource(0, constraintSource);
            } else {
                constraint.AddSource(constraintSource);
            }
        }
    }
}