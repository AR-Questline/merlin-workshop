// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only]
    /// A system that procedurally gathers animations throughout the hierarchy without needing explicit references.
    /// </summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/AnimationGatherer
    /// 
    public class AnimationGatherer : IAnimationClipCollection
    {
        /************************************************************************************************************************/
        #region Recursion Guard
        /************************************************************************************************************************/

        private const int MaxFieldDepth = 7;

        /************************************************************************************************************************/

        private static readonly HashSet<object>
            RecursionGuard = new HashSet<object>();

        private static int _CallCount;

        private static bool BeginRecursionGuard(object obj)
        {
            return default;
        }

        private static void EndCall()
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Fields and Accessors
        /************************************************************************************************************************/

        /// <summary>All the <see cref="AnimationClip"/>s that have been gathered.</summary>
        public readonly HashSet<AnimationClip> Clips = new HashSet<AnimationClip>();

        /// <summary>All the <see cref="ITransition"/>s that have been gathered.</summary>
        public readonly HashSet<ITransition> Transitions = new HashSet<ITransition>();

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public void GatherAnimationClips(ICollection<AnimationClip> clips)
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Cache
        /************************************************************************************************************************/

        private static readonly Dictionary<GameObject, AnimationGatherer>
            ObjectToGatherer = new Dictionary<GameObject, AnimationGatherer>();

        /************************************************************************************************************************/

        static AnimationGatherer()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Clears all cached gatherers.</summary>
        public static void ClearCache() => ObjectToGatherer.Clear();

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        /// <summary>Should exceptions thrown while gathering animations be logged? Default is false to ignore them.</summary>
        public static bool logExceptions;

        /// <summary>Logs the `exception` if <see cref="logExceptions"/> is true. Otherwise does nothing.</summary>
        private static void HandleException(Exception exception)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a cached <see cref="AnimationGatherer"/> containing any <see cref="AnimationClip"/>s referenced by
        /// components in the same hierarchy as the `gameObject`. See <see cref="ICharacterRoot"/> for details.
        /// </summary>
        public static AnimationGatherer GatherFromGameObject(GameObject gameObject)
        {
            return default;
        }

        /// <summary>
        /// Fills the `clips` with any <see cref="AnimationClip"/>s referenced by components in the same hierarchy as
        /// the `gameObject`. See <see cref="ICharacterRoot"/> for details.
        /// </summary>
        public static void GatherFromGameObject(GameObject gameObject, ICollection<AnimationClip> clips)
        {
        }

        /// <summary>
        /// Fills the `clips` with any <see cref="AnimationClip"/>s referenced by components in the same hierarchy as
        /// the `gameObject`. See <see cref="ICharacterRoot"/> for details.
        /// </summary>
        public static void GatherFromGameObject(GameObject gameObject, ref AnimationClip[] clips, bool sort)
        {
        }

        /************************************************************************************************************************/

        private void GatherFromComponents(GameObject gameObject)
        {
        }

        /************************************************************************************************************************/

        private void GatherFromComponents(List<MonoBehaviour> components)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Gathers all animations from the `source`s fields.</summary>
        private void GatherFromObject(object source, int depth)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Types mapped to a delegate that can quickly gather their clips.</summary>
        private static readonly Dictionary<Type, Action<object, AnimationGatherer>>
            TypeToGathererDelegate = new Dictionary<Type, Action<object, AnimationGatherer>>();

        /// <summary>
        /// Uses reflection to gather <see cref="AnimationClip"/>s from fields on the `source` object.
        /// </summary>
        private void GatherFromFields(object source, int depth)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a delegate to gather <see cref="AnimationClip"/>s from all relevant fields in a given `type`.
        /// </summary>
        private static Action<object, AnimationGatherer> BuildClipGathererDelegate(Type type, int depth)
        {
            return default;
        }

        /************************************************************************************************************************/

        private static bool MightContainAnimations(Type type)
        {
            return default;
        }

        /************************************************************************************************************************/
    }
}

#endif

