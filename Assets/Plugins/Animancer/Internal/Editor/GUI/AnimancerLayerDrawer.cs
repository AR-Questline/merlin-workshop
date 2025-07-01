// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only]
    /// A custom Inspector for an <see cref="AnimancerLayer"/> which sorts and exposes some of its internal values.
    /// </summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/AnimancerLayerDrawer
    /// 
    public class AnimancerLayerDrawer : AnimancerNodeDrawer<AnimancerLayer>
    {
        /************************************************************************************************************************/

        /// <summary>The states in the target layer which have non-zero <see cref="AnimancerNode.Weight"/>.</summary>
        public readonly List<AnimancerState> ActiveStates = new List<AnimancerState>();

        /// <summary>The states in the target layer which have zero <see cref="AnimancerNode.Weight"/>.</summary>
        public readonly List<AnimancerState> InactiveStates = new List<AnimancerState>();

        /************************************************************************************************************************/

        /// <summary>The <see cref="GUIStyle"/> used for the area encompassing this drawer is <see cref="GUISkin.box"/>.</summary>
        protected override GUIStyle RegionStyle => GUI.skin.box;

        /************************************************************************************************************************/
        #region Gathering
        /************************************************************************************************************************/

        /// <summary>
        /// Initializes an editor in the list for each layer in the `animancer`.
        /// <para></para>
        /// The `count` indicates the number of elements actually being used. Spare elements are kept in the list in
        /// case they need to be used again later.
        /// </summary>
        internal static void GatherLayerEditors(AnimancerPlayable animancer, List<AnimancerLayerDrawer> editors, out int count)
        {
            count = default(int);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Sets the target `layer` and sorts its states and their keys into the active/inactive lists.
        /// </summary>
        private void GatherStates(AnimancerLayer layer)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Sorts any entries that use another state as their key to come right after that state.
        /// See <see cref="AnimancerPlayable.Play(AnimancerState, float, FadeMode)"/>.
        /// </summary>
        private static void SortAndGatherKeys(List<AnimancerState> states)
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        /// <summary>Draws the layer's name and weight.</summary>
        protected override void DoLabelGUI(Rect area)
        {
        }

        /************************************************************************************************************************/

        /// <summary>The number of pixels of indentation required to fit the foldout arrow.</summary>
        const float FoldoutIndent = 12;

        /// <inheritdoc/>
        protected override void DoFoldoutGUI(Rect area)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void DoDetailsGUI()
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Draws controls for <see cref="AnimancerLayer.IsAdditive"/> and <see cref="AnimancerLayer._Mask"/>.
        /// </summary>
        private void DoLayerDetailsGUI()
        {
        }

        /************************************************************************************************************************/

        private void DoStatesGUI()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Draws all `states` in the given list.</summary>
        private void DoStatesGUI(string label, List<AnimancerState> states)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Cached Inspectors that have already been created for states.</summary>
        private readonly Dictionary<AnimancerState, IAnimancerNodeDrawer>
            StateInspectors = new Dictionary<AnimancerState, IAnimancerNodeDrawer>();

        /// <summary>Draws the Inspector for the given `state`.</summary>
        private void DoStateGUI(AnimancerState state)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Draws all child states of the `state`.</summary>
        private void DoChildStatesGUI(AnimancerState state)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void DoHeaderGUI()
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void DoGUI()
        {
        }

        /// <summary>
        /// If <see cref="AnimationClip"/>s or <see cref="IAnimationClipSource"/>s are dropped inside the `dropArea`,
        /// this method creates a new state in the `target` for each animation.
        /// </summary>
        public static void HandleDragAndDropAnimations(Rect dropArea, IAnimancerComponent target, int layerIndex)
        {
        }

        /************************************************************************************************************************/
        #region Context Menu
        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void PopulateContextMenu(GenericMenu menu)
        {
        }

        /************************************************************************************************************************/

        private bool HasAnyStates(Func<AnimancerState, bool> condition)
        {
            return default;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

