// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Animancer
{
    /// <summary>
    /// Enforces various rules throughout the system, most of which are compiled out if UNITY_ASSERTIONS is not defined
    /// (by default, it is only defined in the Unity Editor and in Development Builds).
    /// </summary>
    /// https://kybernetik.com.au/animancer/api/Animancer/Validate
    /// 
    public static partial class Validate
    {
        /************************************************************************************************************************/

        /// <summary>[Assert-Conditional] Throws if the `clip` is marked as <see cref="AnimationClip.legacy"/>.</summary>
        /// <exception cref="ArgumentException"/>
        [System.Diagnostics.Conditional(Strings.Assertions)]
        public static void AssertNotLegacy(AnimationClip clip)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Assert-Conditional] Throws if the <see cref="AnimancerNode.Root"/> is not the `root`.</summary>
        /// <exception cref="ArgumentException"/>
        [System.Diagnostics.Conditional(Strings.Assertions)]
        public static void AssertRoot(AnimancerNode node, AnimancerPlayable root)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Assert-Conditional] Throws if the `node`'s <see cref="Playable"/> is invalid.</summary>
        /// <exception cref="InvalidOperationException"/>
        [System.Diagnostics.Conditional(Strings.Assertions)]
        public static void AssertPlayable(AnimancerNode node)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Assert-Conditional]
        /// Throws if the `state` was not actually assigned to its specified <see cref="AnimancerNode.Index"/> in
        /// the `states`.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="IndexOutOfRangeException">
        /// The <see cref="AnimancerNode.Index"/> is larger than the number of `states`.
        /// </exception>
        [System.Diagnostics.Conditional(Strings.Assertions)]
        public static void AssertCanRemoveChild(AnimancerState state, IList<AnimancerState> childStates, int childCount)
        {
        }

        /************************************************************************************************************************/
    }
}

