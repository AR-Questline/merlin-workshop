using UnityEngine;

namespace Awaken.Utility.Attributes {
    [UnityEngine.Scripting.Preserve]
    public static class ValidateInputHelpers {
        public const string NotNull = nameof(ValidateInputHelpers._NotNull);
        public static bool _NotNull(Object target) {
            return target;
        }
    }
}