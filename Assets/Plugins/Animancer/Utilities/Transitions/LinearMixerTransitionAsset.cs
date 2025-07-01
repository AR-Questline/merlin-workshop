// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animancer
{
    /// <inheritdoc/>
    /// https://kybernetik.com.au/animancer/api/Animancer/LinearMixerTransitionAsset
    [CreateAssetMenu(menuName = Strings.MenuPrefix + "Mixer Transition/Linear", order = Strings.AssetMenuOrder + 3)]
    [HelpURL(Strings.DocsURLs.APIDocumentation + "/" + nameof(LinearMixerTransitionAsset))]
    public class LinearMixerTransitionAsset : AnimancerTransitionAsset<LinearMixerTransition>
    {
        /// <inheritdoc/>
        [Serializable]
        public new class UnShared :
            UnShared<LinearMixerTransitionAsset, LinearMixerTransition, LinearMixerState>,
            LinearMixerState.ITransition
        { }
    }

    /// <inheritdoc/>
    /// https://kybernetik.com.au/animancer/api/Animancer/LinearMixerTransition
    [Serializable]
    public class LinearMixerTransition : MixerTransition<LinearMixerState, float>,
        LinearMixerState.ITransition, ICopyable<LinearMixerTransition>
    {
        /************************************************************************************************************************/

        [SerializeField]
        [Tooltip("Should setting the Parameter above the highest threshold increase the Speed of the mixer proportionally?")]
        private bool _ExtrapolateSpeed = false;

        /// <summary>[<see cref="SerializeField"/>]
        /// Should setting the <see cref="MixerState{TParameter}.Parameter"/> above the highest threshold increase the
        /// <see cref="AnimancerNode.Speed"/> of the mixer proportionally?
        /// </summary>
        public ref bool ExtrapolateSpeed => ref _ExtrapolateSpeed;

        /************************************************************************************************************************/

        /// <summary>
        /// Are all <see cref="ManualMixerTransition{TMixer}.Animations"/> assigned and
        /// <see cref="MixerTransition{TMixer, TParameter}.Thresholds"/> unique and sorted in ascending order?
        /// </summary>
        public override bool IsValid
        {
            get
            {
                if (!base.IsValid)
                    return false;

                var previous = float.NegativeInfinity;

                var thresholds = Thresholds;
                for (int i = 0; i < thresholds.Length; i++)
                {
                    var threshold = thresholds[i];
                    if (threshold < previous)
                        return false;
                    else
                        previous = threshold;
                }

                return true;
            }
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override LinearMixerState CreateState()
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void Apply(AnimancerState state)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Sorts all states so that their thresholds go from lowest to highest.</summary>
        /// <remarks>This method uses Bubble Sort which is inefficient for large numbers of states.</remarks>
        public void SortByThresholds()
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public virtual void CopyFrom(LinearMixerTransition copyFrom)
        {
        }

        /************************************************************************************************************************/
        #region Drawer
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <inheritdoc/>
        [UnityEditor.CustomPropertyDrawer(typeof(LinearMixerTransition), true)]
        public class Drawer : MixerTransitionDrawer
        {
            /************************************************************************************************************************/

            private static GUIContent _SortingErrorContent;
            private static GUIStyle _SortingErrorStyle;

            /// <inheritdoc/>
            protected override void DoThresholdGUI(Rect area, int index)
            {
            }

            /************************************************************************************************************************/

            /// <inheritdoc/>
            protected override void AddThresholdFunctionsToMenu(UnityEditor.GenericMenu menu)
            {
            }

            /************************************************************************************************************************/

            private void AddCalculateThresholdsFunction(UnityEditor.GenericMenu menu, string label,
                Func<Object, float, float> calculateThreshold)
            {
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
#endif
        #endregion
        /************************************************************************************************************************/
    }
}
