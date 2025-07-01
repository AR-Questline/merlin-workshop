// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using UnityEngine;

namespace Animancer
{
    /// <summary>An <see cref="ITransition"/> with some additional details (mainly for the Unity Editor GUI).</summary>
    /// <remarks>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/transitions">Transitions</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer/ITransitionDetailed
    /// 
    public interface ITransitionDetailed : ITransition
    {
        /************************************************************************************************************************/

        /// <summary>Can this transition create a valid <see cref="AnimancerState"/>?</summary>
        bool IsValid { get; }

        /// <summary>What will the value of <see cref="AnimancerState.IsLooping"/> be for the created state?</summary>
        bool IsLooping { get; }

        /// <summary>The <see cref="AnimancerState.NormalizedTime"/> to start the animation at.</summary>
        /// <remarks><see cref="float.NaN"/> allows the animation to continue from its current time.</remarks>
        float NormalizedStartTime { get; set; }

        /// <summary>The maximum amount of time the animation is expected to take (in seconds).</summary>
        /// <remarks>The actual duration can vary in states like <see cref="ManualMixerState"/>.</remarks>
        float MaximumDuration { get; }

        /// <summary>The <see cref="AnimancerNode.Speed"/> to play the animation at.</summary>
        float Speed { get; set; }

        /************************************************************************************************************************/
    }

    /// https://kybernetik.com.au/animancer/api/Animancer/AnimancerUtilities
    public static partial class AnimancerUtilities
    {
        /************************************************************************************************************************/

        /// <summary>Returns the <see cref="ITransitionDetailed.IsValid"/> with support for <see cref="IWrapper"/>.</summary>
        public static bool IsValid(this ITransition transition)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Outputs the <see cref="Motion.isLooping"/> or <see cref="ITransitionDetailed.IsLooping"/>.</summary>
        /// <remarks>Returns false if the `motionOrTransition` is null or an unsupported type.</remarks>
        public static bool TryGetIsLooping(object motionOrTransition, out bool isLooping)
        {
            isLooping = default(bool);
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Outputs the <see cref="AnimationClip.length"/> or <see cref="ITransitionDetailed.MaximumDuration"/>.</summary>
        /// <remarks>Returns false if the `motionOrTransition` is null or an unsupported type.</remarks>
        public static bool TryGetLength(object motionOrTransition, out float length)
        {
            length = default(float);
            return default;
        }

        /************************************************************************************************************************/
    }
}

