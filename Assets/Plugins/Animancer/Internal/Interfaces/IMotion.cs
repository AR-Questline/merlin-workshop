// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using UnityEngine;

namespace Animancer
{
    /// <summary>An object with an <see cref="AverageAngularSpeed"/> and <see cref="AverageVelocity"/>.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer/IMotion
    /// 
    public interface IMotion
    {
        /************************************************************************************************************************/

        /// <summary>The initial <see cref="Motion.averageAngularSpeed"/> that the created state will have.</summary>
        /// <remarks>The actual average can vary in states like <see cref="ManualMixerState"/>.</remarks>
        float AverageAngularSpeed { get; }

        /// <summary>The initial <see cref="Motion.averageSpeed"/> that the created state will have.</summary>
        /// <remarks>The actual average can vary in states like <see cref="ManualMixerState"/>.</remarks>
        Vector3 AverageVelocity { get; }

        /************************************************************************************************************************/
    }

    /// https://kybernetik.com.au/animancer/api/Animancer/AnimancerUtilities
    public static partial class AnimancerUtilities
    {
        /************************************************************************************************************************/

        /// <summary>Outputs the <see cref="Motion.averageAngularSpeed"/> or <see cref="IMotion.AverageAngularSpeed"/>.</summary>
        /// <remarks>Returns false if the `motion` is null or an unsupported type.</remarks>
        public static bool TryGetAverageAngularSpeed(object motion, out float averageAngularSpeed)
        {
            averageAngularSpeed = default(float);
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Outputs the <see cref="Motion.averageSpeed"/> or <see cref="IMotion.AverageVelocity"/>.</summary>
        /// <remarks>Returns false if the `motion` is null or an unsupported type.</remarks>
        public static bool TryGetAverageVelocity(object motion, out Vector3 averageVelocity)
        {
            averageVelocity = default(Vector3);
            return default;
        }

        /************************************************************************************************************************/
    }
}

