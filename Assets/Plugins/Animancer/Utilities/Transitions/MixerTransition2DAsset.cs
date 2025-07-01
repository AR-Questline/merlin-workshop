// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Animancer
{
    /// <inheritdoc/>
    /// https://kybernetik.com.au/animancer/api/Animancer/MixerTransition2DAsset
    [CreateAssetMenu(menuName = Strings.MenuPrefix + "Mixer Transition/2D", order = Strings.AssetMenuOrder + 4)]
    [HelpURL(Strings.DocsURLs.APIDocumentation + "/" + nameof(MixerTransition2DAsset))]
    public class MixerTransition2DAsset : AnimancerTransitionAsset<MixerTransition2D>
    {
        /// <inheritdoc/>
        [Serializable]
        public new class UnShared :
            UnShared<MixerTransition2DAsset, MixerTransition2D, MixerState<Vector2>>,
            ManualMixerState.ITransition2D
        { }
    }

    /// <inheritdoc/>
    /// https://kybernetik.com.au/animancer/api/Animancer/MixerTransition2D
    [Serializable]
    public class MixerTransition2D : MixerTransition<MixerState<Vector2>, Vector2>,
        ManualMixerState.ITransition2D, ICopyable<MixerTransition2D>
    {
        /************************************************************************************************************************/

        /// <summary>A type of <see cref="ManualMixerState"/> that can be created by a <see cref="MixerTransition2D"/>.</summary>
        public enum MixerType
        {
            /// <summary><see cref="CartesianMixerState"/></summary>
            Cartesian,

            /// <summary><see cref="DirectionalMixerState"/></summary>
            Directional,
        }

        [SerializeField]
        private MixerType _Type;

        /// <summary>[<see cref="SerializeField"/>]
        /// The type of <see cref="ManualMixerState"/> that this transition will create.
        /// </summary>
        public ref MixerType Type => ref _Type;

        /************************************************************************************************************************/

        /// <summary>
        /// Creates and returns a new <see cref="CartesianMixerState"/> or <see cref="DirectionalMixerState"/>
        /// depending on the <see cref="Type"/>.
        /// </summary>
        /// <remarks>
        /// Note that using methods like <see cref="AnimancerPlayable.Play(ITransition)"/> will also call
        /// <see cref="ITransition.Apply"/>, so if you call this method manually you may want to call that method
        /// as well. Or you can just use <see cref="AnimancerUtilities.CreateStateAndApply"/>.
        /// <para></para>
        /// This method also assigns it as the <see cref="AnimancerTransition{TState}.State"/>.
        /// </remarks>
        public override MixerState<Vector2> CreateState()
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public virtual void CopyFrom(MixerTransition2D copyFrom)
        {
        }

        /************************************************************************************************************************/
        #region Drawer
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <inheritdoc/>
        [CustomPropertyDrawer(typeof(MixerTransition2D), true)]
        public class Drawer : MixerTransitionDrawer
        {
            /************************************************************************************************************************/

            /// <summary>
            /// Creates a new <see cref="Drawer"/> using the a wider `thresholdWidth` than usual to accomodate
            /// both the X and Y values.
            /// </summary>
            public Drawer() : base(StandardThresholdWidth * 2 + 20) {
            }

            /************************************************************************************************************************/
            #region Threshold Calculation Functions
            /************************************************************************************************************************/

            /// <inheritdoc/>
            protected override void AddThresholdFunctionsToMenu(GenericMenu menu)
            {
            }

            /************************************************************************************************************************/

            private void Initialize4Directions(SerializedProperty property)
            {
            }

            /************************************************************************************************************************/

            private void Initialize8Directions(SerializedProperty property)
            {
            }

            /************************************************************************************************************************/

            private void AddCalculateThresholdsFunction(GenericMenu menu, string label,
                Func<Object, Vector2, Vector2> calculateThreshold)
            {
            }

            /************************************************************************************************************************/

            private void AddCalculateThresholdsFunctionPerAxis(GenericMenu menu, string label,
                Func<Object, float, float> calculateThreshold)
            {
            }

            private void AddCalculateThresholdsFunction(GenericMenu menu, string label, int axis,
                Func<Object, float, float> calculateThreshold)
            {
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
#endif
        #endregion
        /************************************************************************************************************************/
    }
}
