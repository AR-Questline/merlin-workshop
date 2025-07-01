// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using Animancer.Units;
using UnityEditor;
using UnityEngine;
using static Animancer.Editor.AnimancerGUI;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] Draws manual controls for the <see cref="AnimancerPlayable.Graph"/>.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/AnimancerGraphControls
    /// 
    public static class AnimancerGraphControls
    {
        /************************************************************************************************************************/

        /// <summary>Draws manual controls for the <see cref="AnimancerPlayable.Graph"/>.</summary>
        public static void DoGraphGUI(AnimancerPlayable playable, out Rect area)
        {
            area = default(Rect);
        }

        /************************************************************************************************************************/

        private static void DoRootGUI(AnimancerPlayable playable)
        {
        }

        /************************************************************************************************************************/
        #region Add Animation
        /************************************************************************************************************************/

        /// <summary>Are the Add Animation controls active?</summary>
        private static bool _ShowAddAnimation;

        /************************************************************************************************************************/

        /// <summary>Adds a function to show or hide the "Add Animation" field.</summary>
        public static void AddAddAnimationFunction(GenericMenu menu)
        {
        }

        /************************************************************************************************************************/

        private static void DoAddAnimationGUI(AnimancerPlayable playable)
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

