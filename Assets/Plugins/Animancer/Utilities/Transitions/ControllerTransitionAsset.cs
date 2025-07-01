// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;
using Animancer.Units;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace Animancer
{
    /// <inheritdoc/>
    /// https://kybernetik.com.au/animancer/api/Animancer/ControllerTransitionAsset
    [CreateAssetMenu(menuName = Strings.MenuPrefix + "Controller Transition/Base", order = Strings.AssetMenuOrder + 5)]
    [HelpURL(Strings.DocsURLs.APIDocumentation + "/" + nameof(ControllerTransitionAsset))]
    public class ControllerTransitionAsset : AnimancerTransitionAsset<ControllerTransition>
    {
        /// <inheritdoc/>
        [Serializable]
        public new class UnShared :
            UnShared<ControllerTransitionAsset, ControllerTransition, ControllerState>,
            ControllerState.ITransition
        { }
    }

    /************************************************************************************************************************/

    /// <inheritdoc/>
    /// https://kybernetik.com.au/animancer/api/Animancer/ControllerTransition_1
    [Serializable]
    public abstract class ControllerTransition<TState> : AnimancerTransition<TState>,
        IAnimationClipCollection, ICopyable<ControllerTransition<TState>>
        where TState : ControllerState
    {
        /************************************************************************************************************************/

        [SerializeField]
        private RuntimeAnimatorController _Controller;

        /// <summary>[<see cref="SerializeField"/>]
        /// The <see cref="ControllerState.Controller"/> that will be used for the created state.
        /// </summary>
        public ref RuntimeAnimatorController Controller => ref _Controller;

        /// <inheritdoc/>
        public override Object MainObject => _Controller;

#if UNITY_EDITOR
        /// <summary>[Editor-Only] The name of the serialized backing field of <see cref="Controller"/>.</summary>
        public const string ControllerFieldName = nameof(_Controller);
#endif

        /************************************************************************************************************************/

        [SerializeField]
        [Tooltip("Determines what each layer does when " +
            nameof(ControllerState) + "." + nameof(ControllerState.Stop) + " is called." +
            "\n• If empty, all layers will reset to their default state." +
            "\n• If this array is smaller than the layer count, any additional layers will use the last value in this array.")]
        private ControllerState.ActionOnStop[] _ActionsOnStop;

        /// <summary>[<see cref="SerializeField"/>]
        /// Determines what each layer does when <see cref="ControllerState.Stop"/> is called.
        /// </summary>
        /// <remarks>
        /// If empty, all layers will reset to their <see cref="ControllerState.ActionOnStop.DefaultState"/>.
        /// <para></para>
        /// If this array is smaller than the
        /// <see cref="UnityEngine.Animations.AnimatorControllerPlayable.GetLayerCount"/>, any additional
        /// layers will use the last value in this array.
        /// </remarks>
        public ref ControllerState.ActionOnStop[] ActionsOnStop => ref _ActionsOnStop;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override float MaximumDuration
        {
            get
            {
                if (_Controller == null)
                    return 0;

                var duration = 0f;

                var clips = _Controller.animationClips;
                for (int i = 0; i < clips.Length; i++)
                {
                    var length = clips[i].length;
                    if (duration < length)
                        duration = length;
                }

                return duration;
            }
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override bool IsValid => _Controller != null;

        /************************************************************************************************************************/

        /// <summary>Returns the <see cref="Controller"/>.</summary>
        public static implicit operator RuntimeAnimatorController(ControllerTransition<TState> transition)
            => transition?._Controller;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void Apply(AnimancerState state)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Adds all clips in the <see cref="Controller"/> to the collection.</summary>
        void IAnimationClipCollection.GatherAnimationClips(ICollection<AnimationClip> clips)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public virtual void CopyFrom(ControllerTransition<TState> copyFrom)
        {
        }

        /************************************************************************************************************************/
    }

    /************************************************************************************************************************/

    /// <inheritdoc/>
    /// https://kybernetik.com.au/animancer/api/Animancer/ControllerTransition
    [Serializable]
    public class ControllerTransition : ControllerTransition<ControllerState>,
        ControllerState.ITransition, ICopyable<ControllerTransition>
    {
        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override ControllerState CreateState()
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="ControllerTransition"/>.</summary>
        public ControllerTransition()
        {
        }

        /// <summary>Creates a new <see cref="ControllerTransition"/> with the specified Animator Controller.</summary>
        public ControllerTransition(RuntimeAnimatorController controller) => Controller = controller;

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="ControllerTransition"/> with the specified Animator Controller.</summary>
        public static implicit operator ControllerTransition(RuntimeAnimatorController controller)
            => new ControllerTransition(controller);

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public virtual void CopyFrom(ControllerTransition copyFrom)
        {
        }

        /************************************************************************************************************************/
        #region Drawer
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <inheritdoc/>
        [CustomPropertyDrawer(typeof(ControllerTransition<>), true)]
        [CustomPropertyDrawer(typeof(ControllerTransition), true)]
        public class Drawer : Editor.TransitionDrawer
        {
            /************************************************************************************************************************/

            private readonly string[] Parameters;
            private readonly string[] ParameterPropertySuffixes;

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="Drawer"/> without any parameters.</summary>
            public Drawer() : base(ControllerFieldName) {
            }

            /// <summary>Creates a new <see cref="Drawer"/> and sets the <see cref="Parameters"/>.</summary>
            public Drawer(params string[] parameters) : base(ControllerFieldName)
            {
            }

            /************************************************************************************************************************/

            /// <inheritdoc/>
            protected override void DoChildPropertyGUI(
                ref Rect area,
                SerializedProperty rootProperty,
                SerializedProperty property,
                GUIContent label)
            {
            }

            /************************************************************************************************************************/

            /// <summary>Draws a dropdown menu to select the name of a parameter in the `controller`.</summary>
            protected void DoParameterGUI(Rect area, AnimatorController controller, SerializedProperty property)
            {
            }

            /************************************************************************************************************************/

            private static bool HasFloatParameter(AnimatorController controller, string name)
            {
                return default;
            }

            /************************************************************************************************************************/

            private static string GetFirstFloatParameterName(AnimatorController controller)
            {
                return default;
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
#endif
        #endregion
        /************************************************************************************************************************/
    }
}
