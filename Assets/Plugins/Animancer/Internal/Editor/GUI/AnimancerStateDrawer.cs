// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using static Animancer.Editor.AnimancerPlayableDrawer;
using Object = UnityEngine.Object;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] Draws the Inspector GUI for an <see cref="AnimancerState"/>.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/AnimancerStateDrawer_1
    /// 
    public class AnimancerStateDrawer<T> : AnimancerNodeDrawer<T> where T : AnimancerState
    {
        /************************************************************************************************************************/

        /// <summary>
        /// Creates a new <see cref="AnimancerStateDrawer{T}"/> to manage the Inspector GUI for the `target`.
        /// </summary>
        public AnimancerStateDrawer(T target)
            => Target = target;

        /************************************************************************************************************************/

        /// <summary>The <see cref="GUIStyle"/> used for the area encompassing this drawer is <c>null</c>.</summary>
        protected override GUIStyle RegionStyle
            => null;

        /************************************************************************************************************************/

        /// <summary>Determines whether the <see cref="AnimancerState.MainObject"/> field can occupy the whole line.</summary>
        private bool IsAssetUsedAsKey
            => string.IsNullOrEmpty(Target.DebugName)
            && (Target.Key == null || ReferenceEquals(Target.Key, Target.MainObject));

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override bool AutoNormalizeSiblingWeights
            => AutoNormalizeWeights;

        /************************************************************************************************************************/

        /// <summary>
        /// Draws the state's main label: an <see cref="Object"/> field if it has a
        /// <see cref="AnimancerState.MainObject"/>, otherwise just a simple text label.
        /// <para></para>
        /// Also shows a bar to indicate its progress.
        /// </summary>
        protected override void DoLabelGUI(Rect area)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Draws a progress bar to show the animation time.</summary>
        public static void DoTimeHighlightBarGUI(Rect area, bool isPlaying, float weight, float time, float length, bool isLooping)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Handles Ctrl + Click on the label to CrossFade the animation.</summary>
        private void HandleLabelClick(Rect area)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void DoFoldoutGUI(Rect area)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Gets the current <see cref="AnimancerState.Time"/>.
        /// If the state is looping, the value is modulo by the <see cref="AnimancerState.Length"/>.
        /// </summary>
        private float GetWrappedTime(out float length) => GetWrappedTime(Target.Time, length = Target.Length, Target.IsLooping);

        /// <summary>
        /// Gets the current <see cref="AnimancerState.Time"/>.
        /// If the state is looping, the value is modulo by the <see cref="AnimancerState.Length"/>.
        /// </summary>
        private static float GetWrappedTime(float time, float length, bool isLooping)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void DoDetailsGUI()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Draws a slider for controlling the current <see cref="AnimancerState.Time"/>.</summary>
        private void DoTimeSliderGUI()
        {
        }

        /************************************************************************************************************************/

        private bool DoNormalizedTimeToggle(ref Rect area)
        {
            return default;
        }

        /************************************************************************************************************************/

        private static ConversionCache<int, string> _LoopCounterCache;

        private void DoLoopCounterGUI(ref Rect area, float length)
        {
        }

        /************************************************************************************************************************/

        private void DoOnEndGUI()
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

        /// <summary>Adds the details of this state to the `menu`.</summary>
        protected virtual void AddContextMenuFunctions(GenericMenu menu)
        {
        }

        /************************************************************************************************************************/

        private void AddEventFunctions(GenericMenu menu, string name, AnimancerEvent animancerEvent,
            GenericMenu.MenuFunction clearEvent, GenericMenu.MenuFunction removeEvent)
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

