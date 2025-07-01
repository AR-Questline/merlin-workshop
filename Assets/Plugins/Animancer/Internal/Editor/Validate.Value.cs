// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

namespace Animancer
{
    /// https://kybernetik.com.au/animancer/api/Animancer/Validate
    public static partial class Validate
    {
        /************************************************************************************************************************/

        /// <summary>A rule that defines which values are valid.</summary>
        /// https://kybernetik.com.au/animancer/api/Animancer/Value
        public enum Value
        {
            /// <summary>Any value is allowed.</summary>
            Any,

            /// <summary>Only values between 0 (inclusive) and 1 (inclusive) are allowed.</summary>
            ZeroToOne,

            /// <summary>Only 0 or positive values are allowed.</summary>
            IsNotNegative,

            /// <summary>Infinity and NaN are not allowed.</summary>
            IsFinite,

            /// <summary>Infinity is not allowed.</summary>
            IsFiniteOrNaN,
        }

        /************************************************************************************************************************/

        /// <summary>Enforces the `rule` on the `value`.</summary>
        public static void ValueRule(ref float value, Value rule)
        {
        }

        /************************************************************************************************************************/
    }
}

