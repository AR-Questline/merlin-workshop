// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;
using static Animancer.Editor.AnimancerGUI;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] Draws the Inspector GUI for an <see cref="AnimancerNode"/>.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/IAnimancerNodeDrawer
    /// 
    public interface IAnimancerNodeDrawer
    {
        /// <summary>Draws the details and controls for the target node in the Inspector.</summary>
        void DoGUI();
    }

    /************************************************************************************************************************/

    /// <summary>[Editor-Only] Draws the Inspector GUI for an <see cref="AnimancerNode"/>.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/AnimancerNodeDrawer_1
    /// 
    public abstract class AnimancerNodeDrawer<T> : IAnimancerNodeDrawer where T : AnimancerNode
    {
        /************************************************************************************************************************/

        /// <summary>The node being managed.</summary>
        public T Target { get; protected set; }

        /// <summary>If true, the details of the <see cref="Target"/> will be expanded in the Inspector.</summary>
        public ref bool IsExpanded => ref Target._IsInspectorExpanded;

        /************************************************************************************************************************/

        /// <summary>The <see cref="GUIStyle"/> used for the area encompassing this drawer.</summary>
        protected abstract GUIStyle RegionStyle { get; }

        /************************************************************************************************************************/

        /// <summary>Draws the details and controls for the target <see cref="Target"/> in the Inspector.</summary>
        public virtual void DoGUI()
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Draws the name and other details of the <see cref="Target"/> in the GUI.
        /// </summary>
        protected virtual void DoHeaderGUI()
        {
        }

        /// <summary>
        /// Draws a field for the <see cref="AnimancerState.MainObject"/> if it has one, otherwise just a simple text
        /// label.
        /// </summary>
        protected abstract void DoLabelGUI(Rect area);

        /// <summary>Draws a foldout arrow to expand/collapse the node details.</summary>
        protected abstract void DoFoldoutGUI(Rect area);

        /// <summary>Draws the details of the <see cref="Target"/> in the GUI.</summary>
        protected abstract void DoDetailsGUI();

        /************************************************************************************************************************/

        /// <summary>
        /// Draws controls for <see cref="AnimancerState.IsPlaying"/>, <see cref="AnimancerNode.Speed"/>, and
        /// <see cref="AnimancerNode.Weight"/>.
        /// </summary>
        protected void DoNodeDetailsGUI()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Indicates whether changing the <see cref="AnimancerNode.Weight"/> should normalize its siblings.</summary>
        protected virtual bool AutoNormalizeSiblingWeights => false;

        private void SetWeight(float weight)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Draws the <see cref="AnimancerNode.FadeSpeed"/> and <see cref="AnimancerNode.TargetWeight"/>.</summary>
        private void DoFadeDetailsGUI()
        {
        }

        /************************************************************************************************************************/
        #region Context Menu
        /************************************************************************************************************************/

        /// <summary>
        /// The menu label prefix used for details about the <see cref="Target"/>.
        /// </summary>
        protected const string DetailsPrefix = "Details/";

        /// <summary>
        /// Checks if the current event is a context menu click within the `clickArea` and opens a context menu with various
        /// functions for the <see cref="Target"/>.
        /// </summary>
        protected void CheckContextMenu(Rect clickArea)
        {
        }

        /// <summary>Adds functions relevant to the <see cref="Target"/>.</summary>
        protected abstract void PopulateContextMenu(GenericMenu menu);

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

