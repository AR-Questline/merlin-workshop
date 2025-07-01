// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using Animancer.Editor;
using UnityEditor;
#endif

namespace Animancer
{
    /// <summary>A <see cref="ClipTransition"/> which gets its clip from a <see cref="DirectionalAnimationSet"/>.</summary>
    /// <remarks>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/playing/directional-sets">Directional Animation Sets</see>
    /// </remarks>
    /// <example><code>
    /// // Leave the Clip field empty in the Inspector and assign its AnimationSet instead.
    /// [SerializeField] private DirectionalClipTransition _Transition;
    /// 
    /// ...
    /// 
    /// // Then you can just call SetDirection and Play it like any other transition.
    /// // All of the transition's details like Fade Duration and Events will be applied to whichever clip is plays.
    /// _Transition.SetDirection(Vector2.right);
    /// _Animancer.Play(_Transition);
    /// </code></example>
    /// https://kybernetik.com.au/animancer/api/Animancer/DirectionalClipTransition
    /// 
    [Serializable]
    public class DirectionalClipTransition : ClipTransition,
        ICopyable<DirectionalClipTransition>
    {
        /************************************************************************************************************************/

        [SerializeField]
        [Tooltip("The animations which used to determine the " + nameof(Clip))]
        private DirectionalAnimationSet _AnimationSet;

        /// <summary>[<see cref="SerializeField"/>] 
        /// The <see cref="DirectionalAnimationSet"/> used to determine the <see cref="ClipTransition.Clip"/>.
        /// </summary>
        public ref DirectionalAnimationSet AnimationSet
            => ref _AnimationSet;

        /// <inheritdoc/>
        public override UnityEngine.Object MainObject
            => _AnimationSet;

        /************************************************************************************************************************/

        /// <summary>Sets the <see cref="ClipTransition.Clip"/> from the <see cref="AnimationSet"/>.</summary>
        public void SetDirection(Vector2 direction)
            => Clip = _AnimationSet.GetClip(direction);

        /// <summary>Sets the <see cref="ClipTransition.Clip"/> from the <see cref="AnimationSet"/>.</summary>
        public void SetDirection(int direction)
            => Clip = _AnimationSet.GetClip(direction);

        /// <summary>Sets the <see cref="ClipTransition.Clip"/> from the <see cref="AnimationSet"/>.</summary>
        public void SetDirection(DirectionalAnimationSet.Direction direction)
            => Clip = _AnimationSet.GetClip(direction);

        /// <summary>Sets the <see cref="ClipTransition.Clip"/> from the <see cref="AnimationSet"/>.</summary>
        public void SetDirection(DirectionalAnimationSet8.Direction direction)
            => Clip = _AnimationSet.GetClip((int)direction);

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void GatherAnimationClips(ICollection<AnimationClip> clips)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public virtual void CopyFrom(DirectionalClipTransition copyFrom)
        {
        }

        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <inheritdoc/>
        [CustomPropertyDrawer(typeof(DirectionalClipTransition), true)]
        public new class Drawer : TransitionDrawer
        {
            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="Drawer"/>.</summary>
            public Drawer() : base(nameof(_AnimationSet)) {
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

            /// <summary>Shows a context menu to choose an <see cref="AnimationClip"/> from the `source`.</summary>
            private void PickAnimation(SerializedProperty property, object source)
            {
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }
}
