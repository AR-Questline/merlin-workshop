// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animancer.Editor
{
    /// <summary>
    /// Context menu functions for generating <see cref="AnimancerTransitionAssetBase"/>s based on the contents of
    /// Animator Controllers.
    /// </summary>
    internal static class TransitionGenerator
    {
        /************************************************************************************************************************/

        /// <summary>
        /// Creates an appropriate type of <see cref="AnimancerTransitionAssetBase"/> from the
        /// <see cref="MenuCommand.context"/>.
        /// </summary>
        [MenuItem("CONTEXT/" + nameof(AnimatorState) + "/Generate Transition")]
        [MenuItem("CONTEXT/" + nameof(BlendTree) + "/Generate Transition")]
        [MenuItem("CONTEXT/" + nameof(AnimatorStateTransition) + "/Generate Transition")]
        [MenuItem("CONTEXT/" + nameof(AnimatorStateMachine) + "/Generate Transitions")]
        private static void GenerateTransition(MenuCommand command)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Creates an <see cref="AnimancerTransitionAssetBase"/> from the `state`.</summary>
        private static Object GenerateTransition(AnimatorState state)
            => GenerateTransition(state, state.motion);

        /************************************************************************************************************************/

        /// <summary>Creates an <see cref="AnimancerTransitionAssetBase"/> from the `motion`.</summary>
        private static Object GenerateTransition(Object originalAsset, Motion motion)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Initializes the `transition` based on the `state`.</summary>
        private static void GetDetailsFromState(AnimatorState state, ITransitionDetailed transition)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Creates an <see cref="AnimancerTransitionAssetBase"/> from the `blendTree`.</summary>
        private static Object GenerateTransition(AnimatorState state, BlendTree blendTree)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Creates an <see cref="AnimancerTransitionAssetBase"/> from the `transition`.</summary>
        private static Object GenerateTransition(AnimatorStateTransition transition)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Creates <see cref="AnimancerTransitionAssetBase"/>s from all states in the `stateMachine`.</summary>
        private static Object GenerateTransitions(AnimatorStateMachine stateMachine)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Creates an <see cref="AnimancerTransitionAssetBase"/> from the `blendTree`.</summary>
        private static Object CreateTransition(BlendTree blendTree)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Initializes the `transition` based on the <see cref="BlendTree.children"/>.</summary>
        private static LinearMixerTransition InitializeChildren1D(BlendTree blendTree)
        {
            return default;
        }

        /// <summary>Initializes the `transition` based on the <see cref="BlendTree.children"/>.</summary>
        private static MixerTransition2D InitializeChildren2D(BlendTree blendTree)
        {
            return default;
        }

        /// <summary>Initializes the `transition` based on the <see cref="BlendTree.children"/>.</summary>
        private static ChildMotion[] InitializeChildren<TTransition, TState>(out TTransition transition, BlendTree blendTree)
            where TTransition : ManualMixerTransition<TState>, new()
            where TState : ManualMixerState
        {
            transition = default(TTransition);
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Saves the `transition` in the same folder as the `originalAsset`.</summary>
        private static void SaveTransition(Object originalAsset, Object transition)
        {
        }

        /************************************************************************************************************************/
    }
}

#endif
