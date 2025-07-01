// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace Animancer
{
    /// <summary>Various extension methods and utilities.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer/AnimancerUtilities
    /// 
    public static partial class AnimancerUtilities
    {
        /************************************************************************************************************************/
        #region Misc
        /************************************************************************************************************************/

        /// <summary>This is Animancer Pro.</summary>
        public const bool IsAnimancerPro = true;

        /************************************************************************************************************************/

        /// <summary>Loops the `value` so that <c>0 &lt;= value &lt; 1</c>.</summary>
        /// <remarks>This is more efficient than using <see cref="Wrap"/> with a <c>length</c> of 1.</remarks>
        public static float Wrap01(float value)
        {
            return default;
        }

        /// <summary>Loops the `value` so that <c>0 &lt;= value &lt; length</c>.</summary>
        /// <remarks>Unike <see cref="Mathf.Repeat"/>, this method will never return the `length`.</remarks>
        public static float Wrap(float value, float length)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Rounds the `value` to the nearest integer using <see cref="MidpointRounding.AwayFromZero"/>.
        /// </summary>
        public static float Round(float value)
            => (float)Math.Round(value, MidpointRounding.AwayFromZero);

        /// <summary>
        /// Rounds the `value` to be a multiple of the `multiple` using <see cref="MidpointRounding.AwayFromZero"/>.
        /// </summary>
        public static float Round(float value, float multiple)
            => Round(value / multiple) * multiple;

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension] Is the `value` not NaN or Infinity?</summary>
        /// <remarks>Newer versions of the .NET framework apparently have a <c>float.IsFinite</c> method.</remarks>
        public static bool IsFinite(this float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        /// <summary>[Animancer Extension] Is the `value` not NaN or Infinity?</summary>
        /// <remarks>Newer versions of the .NET framework apparently have a <c>double.IsFinite</c> method.</remarks>
        public static bool IsFinite(this double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        /// <summary>[Animancer Extension] Are all components of the `value` not NaN or Infinity?</summary>
        public static bool IsFinite(this Vector2 value) => value.x.IsFinite() && value.y.IsFinite();

        /************************************************************************************************************************/

        /// <summary>
        /// If `obj` exists, this method returns <see cref="object.ToString"/>.
        /// Or if it is <c>null</c>, this method returns <c>"Null"</c>.
        /// Or if it is an <see cref="Object"/> that has been destroyed, this method returns <c>"Null (ObjectType)"</c>.
        /// </summary>
        public static string ToStringOrNull(object obj)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Ensures that the length and contents of `copyTo` match `copyFrom`.</summary>
        public static void CopyExactArray<T>(T[] copyFrom, ref T[] copyTo)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension] Swaps <c>array[a]</c> with <c>array[b]</c>.</summary>
        public static void Swap<T>(this T[] array, int a, int b)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension]
        /// Is the `array` <c>null</c> or its <see cref="Array.Length"/> <c>0</c>?
        /// </summary>
        public static bool IsNullOrEmpty<T>(this T[] array) => array == null || array.Length == 0;

        /************************************************************************************************************************/

        /// <summary>
        /// If the `array` is <c>null</c> or its <see cref="Array.Length"/> isn't equal to the specified `length`, this
        /// method creates a new array with that `length` and returns <c>true</c>.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="Array.Resize{T}(ref T[], int)"/>, this method doesn't copy over the contents of the old
        /// `array` into the new one.
        /// </remarks>
        public static bool SetLength<T>(ref T[] array, int length)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension] Is the `node` is not null and <see cref="AnimancerNode.IsValid"/>?</summary>
        public static bool IsValid(this AnimancerNode node) => node != null && node.IsValid;

        /// <summary>[Animancer Extension] Is the `transition` not null and <see cref="ITransitionDetailed.IsValid"/>?</summary>
        public static bool IsValid(this ITransitionDetailed transition) => transition != null && transition.IsValid;

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension] Calls <see cref="ITransition.CreateState"/> and <see cref="ITransition.Apply"/>.</summary>
        public static AnimancerState CreateStateAndApply(this ITransition transition, AnimancerPlayable root = null)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>[Pro-Only] Reconnects the input of the specified `playable` to its output.</summary>
        public static void RemovePlayable(Playable playable, bool destroy = true)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Checks if any <see cref="AnimationClip"/> in the `source` has an animation event with the specified
        /// `functionName`.
        /// </summary>
        public static bool HasEvent(IAnimationClipCollection source, string functionName)
        {
            return default;
        }

        /// <summary>Checks if the `clip` has an animation event with the specified `functionName`.</summary>
        public static bool HasEvent(AnimationClip clip, string functionName)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension] [Pro-Only]
        /// Calculates all thresholds in the `mixer` using the <see cref="AnimancerState.AverageVelocity"/> of each
        /// state on the X and Z axes.
        /// <para></para>
        /// Note that this method requires the <c>Root Transform Position (XZ) -> Bake Into Pose</c> toggle to be
        /// disabled in the Import Settings of each <see cref="AnimationClip"/> in the mixer.
        /// </summary>
        public static void CalculateThresholdsFromAverageVelocityXZ(this MixerState<Vector2> mixer)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Copies the value of the `parameter` from `copyFrom` to `copyTo`.</summary>
        public static void CopyParameterValue(Animator copyFrom, Animator copyTo, AnimatorControllerParameter parameter)
        {
        }

        /// <summary>Copies the value of the `parameter` from `copyFrom` to `copyTo`.</summary>
        public static void CopyParameterValue(AnimatorControllerPlayable copyFrom, AnimatorControllerPlayable copyTo, AnimatorControllerParameter parameter)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Gets the value of the `parameter` in the `animator`.</summary>
        public static object GetParameterValue(Animator animator, AnimatorControllerParameter parameter)
        {
            return default;
        }

        /// <summary>Gets the value of the `parameter` in the `playable`.</summary>
        public static object GetParameterValue(AnimatorControllerPlayable playable, AnimatorControllerParameter parameter)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Sets the `value` of the `parameter` in the `animator`.</summary>
        public static void SetParameterValue(Animator animator, AnimatorControllerParameter parameter, object value)
        {
        }

        /// <summary>Sets the `value` of the `parameter` in the `playable`.</summary>
        public static void SetParameterValue(AnimatorControllerPlayable playable, AnimatorControllerParameter parameter, object value)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a <see cref="NativeArray{T}"/> containing a single element so that it can be used like a reference
        /// in Unity's C# Job system which does not allow regular reference types.
        /// </summary>
        /// <remarks>Note that you must call <see cref="NativeArray{T}.Dispose()"/> when you're done with the array.</remarks>
        public static NativeArray<T> CreateNativeReference<T>() where T : struct
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a <see cref="NativeArray{T}"/> of <see cref="TransformStreamHandle"/>s for each of the `transforms`.
        /// </summary>
        /// <remarks>Note that you must call <see cref="NativeArray{T}.Dispose()"/> when you're done with the array.</remarks>
        public static NativeArray<TransformStreamHandle> ConvertToTransformStreamHandles(
            IList<Transform> transforms, Animator animator)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Returns a string stating that the `value` is unsupported.</summary>
        public static string GetUnsupportedMessage<T>(T value)
            => $"Unsupported {typeof(T).FullName}: {value}";

        /// <summary>Returns an exception stating that the `value` is unsupported.</summary>
        public static ArgumentException CreateUnsupportedArgumentException<T>(T value)
            => new ArgumentException(GetUnsupportedMessage(value));

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Components
        /************************************************************************************************************************/

        /// <summary>[Animancer Extension]
        /// Adds the specified type of <see cref="IAnimancerComponent"/>, links it to the `animator`, and returns it.
        /// </summary>
        public static T AddAnimancerComponent<T>(this Animator animator) where T : Component, IAnimancerComponent
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension]
        /// Returns the <see cref="IAnimancerComponent"/> on the same <see cref="GameObject"/> as the `animator` if
        /// there is one. Otherwise this method adds a new one and returns it.
        /// </summary>
        public static T GetOrAddAnimancerComponent<T>(this Animator animator) where T : Component, IAnimancerComponent
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the first <typeparamref name="T"/> component on the `gameObject` or its parents or children (in
        /// that order).
        /// </summary>
        public static T GetComponentInParentOrChildren<T>(this GameObject gameObject) where T : class
        {
            return default;
        }

        /// <summary>
        /// If the `component` is <c>null</c>, this method tries to find one on the `gameObject` or its parents or
        /// children (in that order).
        /// </summary>
        public static bool GetComponentInParentOrChildren<T>(this GameObject gameObject, ref T component) where T : class
        {
            return default;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Editor
        /************************************************************************************************************************/

        /// <summary>[Assert-Conditional]
        /// Throws an <see cref="UnityEngine.Assertions.AssertionException"/> if the `condition` is false.
        /// </summary>
        /// <remarks>
        /// This method is similar to <see cref="Debug.Assert(bool, object)"/>, but it throws an exception instead of
        /// just logging the `message`.
        /// </remarks>
        [System.Diagnostics.Conditional(Strings.Assertions)]
        public static void Assert(bool condition, object message)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Conditional] Indicates that the `target` needs to be re-serialized.</summary>
        [System.Diagnostics.Conditional(Strings.UnityEditor)]
        public static void SetDirty(Object target)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Conditional]
        /// Applies the effects of the animation `clip` to the <see cref="Component.gameObject"/>.
        /// </summary>
        /// <remarks>This method is safe to call during <see cref="MonoBehaviour"/><c>.OnValidate</c>.</remarks>
        /// <param name="clip">The animation to apply. If <c>null</c>, this method does nothing.</param>
        /// <param name="component">
        /// The animation will be applied to an <see cref="Animator"/> or <see cref="Animation"/> component on the same
        /// object as this or on any of its parents or children. If <c>null</c>, this method does nothing.
        /// </param>
        /// <param name="time">Determines which part of the animation to apply (in seconds).</param>
        /// <seealso cref="EditModePlay"/>
        [System.Diagnostics.Conditional(Strings.UnityEditor)]
        public static void EditModeSampleAnimation(this AnimationClip clip, Component component, float time = 0)
        {
        }

        private static bool ShouldEditModeSample(AnimationClip clip, Component component)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Conditional] Plays the specified `clip` if called in Edit Mode.</summary>
        /// <remarks>This method is safe to call during <see cref="MonoBehaviour"/><c>.OnValidate</c>.</remarks>
        /// <param name="clip">The animation to apply. If <c>null</c>, this method does nothing.</param>
        /// <param name="component">
        /// The animation will be played on an <see cref="IAnimancerComponent"/> on the same object as this or on any
        /// of its parents or children. If <c>null</c>, this method does nothing.
        /// </param>
        /// <seealso cref="EditModeSampleAnimation"/>
        [System.Diagnostics.Conditional(Strings.UnityEditor)]
        public static void EditModePlay(this AnimationClip clip, Component component)
        {
        }

        private static bool ShouldEditModePlay(IAnimancerComponent animancer, AnimationClip clip)
        {
            return default;
        }

        /************************************************************************************************************************/
#if UNITY_ASSERTIONS
        /************************************************************************************************************************/

        private static System.Reflection.FieldInfo _DelegatesField;
        private static bool _GotDelegatesField;

        /// <summary>[Assert-Only]
        /// Uses reflection to achieve the same as <see cref="Delegate.GetInvocationList"/> without allocating
        /// garbage every time.
        /// <list type="bullet">
        /// <item>If the delegate is <c>null</c> or , this method returns <c>false</c> and outputs <c>null</c>.</item>
        /// <item>If the underlying <c>delegate</c> field was not found, this method returns <c>false</c> and outputs <c>null</c>.</item>
        /// <item>If the delegate is not multicast, this method this method returns <c>true</c> and outputs <c>null</c>.</item>
        /// <item>If the delegate is multicast, this method this method returns <c>true</c> and outputs its invocation list.</item>
        /// </list>
        /// </summary>
        public static bool TryGetInvocationListNonAlloc(MulticastDelegate multicast, out Delegate[] delegates)
        {
            delegates = default(Delegate[]);
            return default;
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

