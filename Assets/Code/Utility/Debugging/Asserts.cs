using System;
using System.Diagnostics;
using Unity.Burst;
using UnityEngine.Assertions;

namespace Awaken.Utility.Debugging {
    public static class Asserts {
        /// <summary>
        /// Checks if <paramref name="a"/> is less <paramref name="b"/>
        /// </summary>
        /// <remarks>
        /// Must <paramref name="a"/> &lt; <paramref name="b"/>, otherwise assertion fails.
        /// </remarks>
        [Conditional("UNITY_ASSERTIONS"), Conditional("AR_DEBUG"), Conditional("DEBUG")]
        public static void IsLessThan<T>(T a, T b, string userMessage = "") where T : System.IComparable<T> {
            if (a.CompareTo(b) >= 0) {
                HandleAssertionFailed($"{a} is greater or equal to {b}", userMessage);
            }
        }

        /// <summary>
        /// Checks if <paramref name="a"/> is less than or equal to <paramref name="b"/>.
        /// </summary>
        /// <remarks>
        /// Must <paramref name="a"/> &lt;= <paramref name="b"/>, otherwise assertion fails.
        /// </remarks>
        [Conditional("UNITY_ASSERTIONS"), Conditional("AR_DEBUG"), Conditional("DEBUG")]
        public static void IsLessOrEqual<T>(T a, T b, string userMessage = "") where T : System.IComparable<T> {
            if (a.CompareTo(b) > 0) {
                HandleAssertionFailed($"{a} is greater than {b}", userMessage);
            }
        }

        /// <summary>
        /// Checks if <paramref name="a"/> is greater <paramref name="b"/>
        /// </summary>
        /// <remarks>
        /// Must <paramref name="a"/> &gt; <paramref name="b"/>, otherwise assertion fails.
        /// </remarks>
        [Conditional("UNITY_ASSERTIONS"), Conditional("AR_DEBUG"), Conditional("DEBUG")]
        public static void IsGreaterThan<T>(T a, T b, string userMessage = "") where T : System.IComparable<T> {
            if (b.CompareTo(a) >= 0) {
                HandleAssertionFailed($"{a} is less or equal to {b}", userMessage);
            }
        }

        /// <summary>
        /// Checks if <paramref name="a"/> is greater or equal to <paramref name="b"/>
        /// </summary>
        /// <remarks>
        /// Must <paramref name="a"/> &gt;= <paramref name="b"/>, otherwise assertion fails.
        /// </remarks>
        [Conditional("UNITY_ASSERTIONS"), Conditional("AR_DEBUG"), Conditional("DEBUG")]
        public static void IsGreaterOrEqual<T>(T a, T b, string userMessage = "") where T : System.IComparable<T> {
            if (b.CompareTo(a) > 0) {
                HandleAssertionFailed($"{a} is less than {b}", userMessage);
            }
        }

        /// <summary>
        /// Checks if <paramref name="index"/> is in the range [0, <paramref name="count"/>).
        /// </summary>
        /// <remarks>
        /// uint cannot be negative, so only check if it is less than <paramref name="count"/>.
        /// </remarks>
        [Conditional("UNITY_ASSERTIONS"), Conditional("AR_DEBUG"), Conditional("DEBUG")]
        public static void IndexInRange(uint index, uint count, string userMessage = "") {
            if (index >= count) {
                HandleAssertionFailed($"Index {index} is out of range [0-{count})", userMessage);
            }
        }

        /// <summary>
        /// Checks if <paramref name="index"/> is in the range [0, <paramref name="count"/>).
        /// </summary>
        [Conditional("UNITY_ASSERTIONS"), Conditional("AR_DEBUG"), Conditional("DEBUG")]
        public static void IndexInRange(int index, uint count, string userMessage = "") {
            if (index >= count) {
                HandleAssertionFailed($"Index {index} is out of range [0-{count}). {index} is greater or equal to upper limit", userMessage);
            }

            if (index < 0) {
                HandleAssertionFailed($"Index {index} is out of range [0-{count}). {index} is negative", userMessage);
            }
        }

        /// <summary>
        /// Checks if <paramref name="count"/> is in domain [0; int.MaxValue].
        /// Checks if <paramref name="index"/> is in the range [0, <paramref name="count"/>).
        /// </summary>
        [Conditional("UNITY_ASSERTIONS"), Conditional("AR_DEBUG"), Conditional("DEBUG")]
        public static void IndexInRange(int index, int count, string userMessage = "") {
            if (count < 0) {
                HandleAssertionFailed($"Count {count} is negative", userMessage);
            }

            if (index >= count) {
                HandleAssertionFailed($"Index {index} is out of range [0-{count}). {index} is greater or equal to upper limit", userMessage);
            }

            if (index < 0) {
                HandleAssertionFailed($"Index {index} is out of range [0-{count}). {index} is negative", userMessage);
            }
        }

        /// <summary>
        /// Checks if <paramref name="count"/> is in domain [0; int.MaxValue].
        /// Checks if <paramref name="index"/> is in the range [0, <paramref name="count"/>).
        /// </summary>
        /// /// <remarks>
        /// uint cannot be negative, so only check if it is less than <paramref name="count"/>.
        /// </remarks>
        [Conditional("UNITY_ASSERTIONS"), Conditional("AR_DEBUG"), Conditional("DEBUG")]
        public static void IndexInRange(uint index, int count, string userMessage = "") {
            if (count < 0) {
                HandleAssertionFailed($"Count {count} is negative", userMessage);
            }

            if (index >= count) {
                HandleAssertionFailed($"Index {index} is out of range [0-{count})", userMessage);
            }
        }

        [Conditional("UNITY_ASSERTIONS"), Conditional("AR_DEBUG"), Conditional("DEBUG")]
        public static void AreEqual<T>(in T left, in T right, string userMessage = "") where T : System.IComparable<T> {
            if (left.CompareTo(right) != 0) {
                HandleAssertionFailed($"{left} is not equal to {right}", userMessage);
            }
        }

        [Conditional("UNITY_ASSERTIONS"), Conditional("AR_DEBUG"), Conditional("DEBUG")]
        public static void IsTrue(bool requirement, string userMessage = "") {
            if (!requirement) {
                HandleAssertionFailed("Requirement is not met, value is false", userMessage);
            }
        }

        [Conditional("UNITY_ASSERTIONS"), Conditional("AR_DEBUG"), Conditional("DEBUG")]
        public static void IsFalse(bool requirement, string userMessage = "") {
            if (requirement) {
                HandleAssertionFailed("Requirement is not met, value is true", userMessage);
            }
        }

        static void HandleAssertionFailed(string message, string userMessage) {
#if AR_DEBUG
            var managedHandled = false;
            ManagedHandleAssertionFailed(message, userMessage, ref managedHandled);
            if (!managedHandled) {
                UnityEngine.Debug.LogError("[CrashAssertionException]");
                UnityEngine.Debug.LogError(message);
                UnityEngine.Debug.LogError(userMessage);
            }
            DebugUtils.Crash();
#else
            throw new CrashAssertionException(message, userMessage);
#endif
        }

        [BurstDiscard]
        static void ManagedHandleAssertionFailed(string message, string userMessage, ref bool handled) {
            var outputMessage = $"[CrashAssertionException]\n{message} \n {userMessage}";
            Log.Critical?.Error(outputMessage);
            handled = true;
        }

        [Serializable]
        public class CrashAssertionException : AssertionException {
            public CrashAssertionException (string message, string userMessage) : base(message, userMessage) {}
        }
    }
}