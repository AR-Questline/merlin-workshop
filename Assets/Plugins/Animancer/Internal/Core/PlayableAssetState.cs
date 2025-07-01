// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Audio;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace Animancer
{
    /// <summary>[Pro-Only] An <see cref="AnimancerState"/> which plays a <see cref="PlayableAsset"/>.</summary>
    /// <remarks>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/timeline">Timeline</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer/PlayableAssetState
    /// 
    public class PlayableAssetState : AnimancerState, ICopyable<PlayableAssetState>
    {
        /************************************************************************************************************************/

        /// <summary>An <see cref="ITransition{TState}"/> that creates a <see cref="PlayableAssetState"/>.</summary>
        public interface ITransition : ITransition<PlayableAssetState> { }

        /************************************************************************************************************************/
        #region Fields and Properties
        /************************************************************************************************************************/

        /// <summary>The <see cref="PlayableAsset"/> which this state plays.</summary>
        private PlayableAsset _Asset;

        /// <summary>The <see cref="PlayableAsset"/> which this state plays.</summary>
        public PlayableAsset Asset
        {
            get => _Asset;
            set => ChangeMainObject(ref _Asset, value);
        }

        /// <summary>The <see cref="PlayableAsset"/> which this state plays.</summary>
        public override Object MainObject
        {
            get => _Asset;
            set => _Asset = (PlayableAsset)value;
        }

        /************************************************************************************************************************/

        private float _Length;

        /// <summary>The <see cref="PlayableAsset.duration"/>.</summary>
        public override float Length => _Length;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void OnSetIsPlaying()
        {
        }

        /************************************************************************************************************************/

        /// <summary>IK cannot be dynamically enabled on a <see cref="PlayableAssetState"/>.</summary>
        public override void CopyIKFlags(AnimancerNode copyFrom) {
        }

        /************************************************************************************************************************/

        /// <summary>IK cannot be dynamically enabled on a <see cref="PlayableAssetState"/>.</summary>
        public override bool ApplyAnimatorIK
        {
            get => false;
            set
            {
#if UNITY_ASSERTIONS
                if (value)
                    OptionalWarning.UnsupportedIK.Log(
                        $"IK cannot be dynamically enabled on a {nameof(PlayableAssetState)}.", Root?.Component);
#endif
            }
        }

        /************************************************************************************************************************/

        /// <summary>IK cannot be dynamically enabled on a <see cref="PlayableAssetState"/>.</summary>
        public override bool ApplyFootIK
        {
            get => false;
            set
            {
#if UNITY_ASSERTIONS
                if (value)
                    OptionalWarning.UnsupportedIK.Log(
                        $"IK cannot be dynamically enabled on a {nameof(PlayableAssetState)}.", Root?.Component);
#endif
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Methods
        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="PlayableAssetState"/> to play the `asset`.</summary>
        /// <exception cref="ArgumentNullException">The `asset` is null.</exception>
        public PlayableAssetState(PlayableAsset asset)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void CreatePlayable(out Playable playable)
        {
            playable = default(Playable);
        }

        /************************************************************************************************************************/

        private IList<Object> _Bindings;
        private bool _HasInitializedBindings;

        /************************************************************************************************************************/

        /// <summary>The objects controlled by each track in the asset.</summary>
        public IList<Object> Bindings
        {
            get => _Bindings;
            set
            {
                _Bindings = value;
                InitializeBindings();
            }
        }

        /************************************************************************************************************************/

        /// <summary>Sets the <see cref="Bindings"/>.</summary>
        public void SetBindings(params Object[] bindings)
        {
        }

        /************************************************************************************************************************/

        private void InitializeBindings()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Should the `binding` be skipped when determining how to map the <see cref="Bindings"/>?</summary>
        public static void GetBindingDetails(PlayableBinding binding, out string name, out Type type, out bool isMarkers)
        {
            name = default(string);
            type = default(Type);
            isMarkers = default(bool);
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void Destroy()
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override AnimancerState Clone(AnimancerPlayable root)
        {
            return default;
        }

        /// <inheritdoc/>
        void ICopyable<PlayableAssetState>.CopyFrom(PlayableAssetState copyFrom)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void AppendDetails(StringBuilder text, string separator)
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

