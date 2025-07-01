using System;
using Awaken.Utility.Extensions;

namespace Awaken.TG.Main.Heroes.Stats.Utils {
    public static class ChangeUtils {
        [UnityEngine.Scripting.Preserve]
        public static bool IsChangeTypeSatisfied(ChangeType type, float changeValue) {
            if (Math.Abs(changeValue) < 0.5f) {
                return type.HasFlagFast(ChangeType.None);
            } else if (type.HasFlagFast(ChangeType.Any)) {
                return true;
            } else if (type.HasFlagFast(ChangeType.Positive) && changeValue > 0) {
                return true;
            } else if (type.HasFlagFast(ChangeType.Negative) && changeValue < 0) {
                return true;
            } else {
                return false;
            }
        }
    }
    
    [Flags]
    public enum ChangeType {
        None = 1 << 0,
        Positive = 1 << 1,
        Negative = 1 << 2,
        Any = Positive | Negative,
    }
}