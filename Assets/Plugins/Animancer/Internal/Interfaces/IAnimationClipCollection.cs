// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Playables;

namespace Animancer
{
    /// <summary>
    /// A variant of <see cref="IAnimationClipSource"/> which uses a <see cref="ICollection{T}"/> instead of a
    /// <see cref="List{T}"/> so that it can take a <see cref="HashSet{T}"/> to efficiently avoid adding duplicates.
    /// <see cref="AnimancerUtilities"/> contains various extension methods for this purpose.
    /// </summary>
    /// <remarks>
    /// <see cref="IAnimationClipSource"/> still needs to be the main point of entry for the Animation Window, so this
    /// interface is only used internally.
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer/IAnimationClipCollection
    /// 
    public interface IAnimationClipCollection
    {
        /************************************************************************************************************************/

        /// <summary>Adds all the animations associated with this object to the `clips`.</summary>
        void GatherAnimationClips(ICollection<AnimationClip> clips);

        /************************************************************************************************************************/
    }

    /************************************************************************************************************************/

    /// https://kybernetik.com.au/animancer/api/Animancer/AnimancerUtilities
    public static partial class AnimancerUtilities
    {
        /************************************************************************************************************************/

        /// <summary>[Animancer Extension]
        /// Adds the `clip` to the `clips` if it wasn't there already.
        /// </summary>
        public static void Gather(this ICollection<AnimationClip> clips, AnimationClip clip)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension]
        /// Calls <see cref="Gather(ICollection{AnimationClip}, AnimationClip)"/> for each of the `newClips`.
        /// </summary>
        public static void Gather(this ICollection<AnimationClip> clips, IList<AnimationClip> gatherFrom)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension]
        /// Calls <see cref="Gather(ICollection{AnimationClip}, AnimationClip)"/> for each of the `newClips`.
        /// </summary>
        public static void Gather(this ICollection<AnimationClip> clips, IEnumerable<AnimationClip> gatherFrom)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension]
        /// Calls <see cref="Gather(ICollection{AnimationClip}, AnimationClip)"/> for each clip in the `asset`.
        /// </summary>
        public static void GatherFromAsset(this ICollection<AnimationClip> clips, PlayableAsset asset)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Gathers all the animations in the `tracks`.</summary>
        private static void GatherFromTracks(ICollection<AnimationClip> clips, IEnumerable tracks)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension]
        /// Calls <see cref="Gather(ICollection{AnimationClip}, AnimationClip)"/> for each clip gathered by
        /// <see cref="IAnimationClipSource.GetAnimationClips"/>.
        /// </summary>
        public static void GatherFromSource(this ICollection<AnimationClip> clips, IAnimationClipSource source)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension]
        /// Calls <see cref="GatherFromSource(ICollection{AnimationClip}, object)"/> for each item in the `source`.
        /// </summary>
        public static void GatherFromSource(this ICollection<AnimationClip> clips, IEnumerable source)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension]
        /// Calls <see cref="Gather(ICollection{AnimationClip}, AnimationClip)"/> for each clip in the `source`,
        /// supporting both <see cref="IAnimationClipSource"/> and <see cref="IAnimationClipCollection"/>.
        /// </summary>
        public static bool GatherFromSource(this ICollection<AnimationClip> clips, object source)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Attempts to get the <see cref="AnimationClip.frameRate"/> from the `clipSource` and returns true if
        /// successful. If it has multiple animations with different rates, this method returns false.
        /// </summary>
        public static bool TryGetFrameRate(object clipSource, out float frameRate)
        {
            frameRate = default(float);
            return default;
        }

        /************************************************************************************************************************/
    }
}

