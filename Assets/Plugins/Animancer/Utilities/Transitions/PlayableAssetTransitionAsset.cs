// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using Animancer.Units;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using Animancer.Editor;
using UnityEditor;
#endif

namespace Animancer
{
    /// <inheritdoc/>
    /// https://kybernetik.com.au/animancer/api/Animancer/PlayableAssetTransitionAsset
    [CreateAssetMenu(menuName = Strings.MenuPrefix + "Playable Asset Transition", order = Strings.AssetMenuOrder + 9)]
    [HelpURL(Strings.DocsURLs.APIDocumentation + "/" + nameof(PlayableAssetTransitionAsset))]
    public class PlayableAssetTransitionAsset : AnimancerTransitionAsset<PlayableAssetTransition>
    {
        /// <inheritdoc/>
        [Serializable]
        public new class UnShared :
            UnShared<PlayableAssetTransitionAsset, PlayableAssetTransition, PlayableAssetState>,
            PlayableAssetState.ITransition
        { }
    }

    /// <inheritdoc/>
    /// https://kybernetik.com.au/animancer/api/Animancer/PlayableAssetTransition
    [Serializable]
    public class PlayableAssetTransition : AnimancerTransition<PlayableAssetState>,
        PlayableAssetState.ITransition, IAnimationClipCollection, ICopyable<PlayableAssetTransition>
    {
        /************************************************************************************************************************/

        [SerializeField, Tooltip("The asset to play")]
        private PlayableAsset _Asset;

        /// <summary>[<see cref="SerializeField"/>] The asset to play.</summary>
        public ref PlayableAsset Asset => ref _Asset;

        /// <inheritdoc/>
        public override Object MainObject => _Asset;

        /// <summary>
        /// The <see cref="Asset"/> will be used as the <see cref="AnimancerState.Key"/> for the created state to
        /// be registered with.
        /// </summary>
        public override object Key => _Asset;

        /************************************************************************************************************************/

        [SerializeField]
        [Tooltip(Strings.Tooltips.OptionalSpeed)]
        [AnimationSpeed]
        [DefaultValue(1f, -1f)]
        private float _Speed = 1;

        /// <summary>[<see cref="SerializeField"/>]
        /// Determines how fast the animation plays (1x = normal speed, 2x = double speed).
        /// </summary>
        public override float Speed
        {
            get => _Speed;
            set => _Speed = value;
        }

        /************************************************************************************************************************/

        [SerializeField]
        [Tooltip(Strings.Tooltips.NormalizedStartTime)]
        [AnimationTime(AnimationTimeAttribute.Units.Normalized)]
        [DefaultValue(float.NaN, 0f)]
        private float _NormalizedStartTime = float.NaN;

        /// <inheritdoc/>
        public override float NormalizedStartTime
        {
            get => _NormalizedStartTime;
            set => _NormalizedStartTime = value;
        }

        /************************************************************************************************************************/

        [SerializeField]
        [Tooltip("The objects controlled by each of the tracks in the Asset")]
#if UNITY_2020_2_OR_NEWER
        [NonReorderable]
#endif
        private Object[] _Bindings;

        /// <summary>[<see cref="SerializeField"/>] The objects controlled by each of the tracks in the Asset.</summary>
        public ref Object[] Bindings => ref _Bindings;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override float MaximumDuration => _Asset != null ? (float)_Asset.duration : 0;

        /// <inheritdoc/>
        public override bool IsValid => _Asset != null;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override PlayableAssetState CreateState()
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void Apply(AnimancerState state)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Gathers all the animations associated with this object.</summary>
        void IAnimationClipCollection.GatherAnimationClips(ICollection<AnimationClip> clips)
            => clips.GatherFromAsset(_Asset);

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public virtual void CopyFrom(PlayableAssetTransition copyFrom)
        {
        }

        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <inheritdoc/>
        [CustomPropertyDrawer(typeof(PlayableAssetTransition), true)]
        public class Drawer : TransitionDrawer
        {
            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="Drawer"/>.</summary>
            public Drawer() : base(nameof(_Asset)) {
            }

            /************************************************************************************************************************/

            /// <inheritdoc/>
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return default;
            }

            /************************************************************************************************************************/

            private PlayableAsset _CurrentAsset;

            /// <inheritdoc/>
            protected override void DoMainPropertyGUI(Rect area, out Rect labelArea,
                SerializedProperty rootProperty, SerializedProperty mainProperty)
            {
                labelArea = default(Rect);
            }

            /// <inheritdoc/>
            public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
            {
            }

            /// <inheritdoc/>
            protected override void DoChildPropertyGUI(ref Rect area, SerializedProperty rootProperty,
                SerializedProperty property, GUIContent label)
            {
            }

            /************************************************************************************************************************/

            private void DoBindingsGUI(ref Rect area, SerializedProperty property, GUIContent label)
            {
            }

            /************************************************************************************************************************/

            private int GetOutputCount(out IEnumerator<PlayableBinding> outputEnumerator, out bool firstBindingIsAnimation)
            {
                outputEnumerator = default(IEnumerator<PlayableBinding>);
                firstBindingIsAnimation = default(bool);
                return default;
            }

            /************************************************************************************************************************/

            private void DoBindingsCountGUI(Rect area, SerializedProperty property, GUIContent label,
                int outputCount, bool firstBindingIsAnimation, out int bindingCount)
            {
                bindingCount = default(int);
            }

            /************************************************************************************************************************/

            private void DoBindingGUI(Rect area, SerializedProperty property, GUIContent label,
                IEnumerator<PlayableBinding> outputEnumerator, int trackIndex)
            {
            }

            /************************************************************************************************************************/

            private static void DoRemoveButtonIfNecessary(ref Rect area, GUIContent label, SerializedProperty property,
                 int trackIndex, ref Type bindingType, ref Object obj)
            {
            }

            /************************************************************************************************************************/

            private static void DoRemoveButton(ref Rect area, GUIContent label, SerializedProperty property,
                ref Object obj, string tooltip)
            {
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }
}
