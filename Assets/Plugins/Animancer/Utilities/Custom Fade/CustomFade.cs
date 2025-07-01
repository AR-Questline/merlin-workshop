// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System.Collections.Generic;
using UnityEngine;

namespace Animancer
{
    /// <summary>[Pro-Only]
    /// A system which fades animation weights animations using a custom calculation rather than linear interpolation.
    /// </summary>
    /// 
    /// <remarks>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/blending/fading#custom-fade">Custom Fade</see>
    /// </remarks>
    /// 
    /// <example><code>
    /// [SerializeField] private AnimancerComponent _Animancer;
    /// [SerializeField] private AnimationClip _Clip;
    /// 
    /// private void Awake()
    /// {
    ///     // Start fading the animation normally.
    ///     var state = _Animancer.Play(_Clip, 0.25f);
    ///     
    ///     // Then apply the custom fade to modify it.
    ///     CustomFade.Apply(state, Easing.Sine.InOut);// Use a delegate.
    ///     CustomFade.Apply(state, Easing.Function.SineInOut);// Or use the Function enum.
    ///     
    ///     // Or apply it to whatever the current state happens to be.
    ///     CustomFade.Apply(_Animancer, Easing.Sine.InOut);
    ///     
    ///     // Anything else you play after that will automatically cancel the custom fade.
    /// }
    /// </code></example>
    /// 
    /// https://kybernetik.com.au/animancer/api/Animancer/CustomFade
    /// 
    public abstract partial class CustomFade : Key, IUpdatable
    {
        /************************************************************************************************************************/

        private float _Time;
        private float _FadeSpeed;
        private NodeWeight _Target;
        private AnimancerLayer _Layer;
        private int _CommandCount;

        private readonly List<NodeWeight> FadeOutNodes = new List<NodeWeight>();

        /************************************************************************************************************************/

        private readonly struct NodeWeight
        {
            public readonly AnimancerNode Node;
            public readonly float StartingWeight;

            public NodeWeight(AnimancerNode node) : this()
            {
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Gathers the current details of the <see cref="AnimancerNode.Root"/> and register this
        /// <see cref="CustomFade"/> to be updated by it so that it can replace the regular fade behaviour.
        /// </summary>
        protected void Apply(AnimancerState state)
        {
        }

        /// <summary>
        /// Gathers the current details of the <see cref="AnimancerNode.Root"/> and register this
        /// <see cref="CustomFade"/> to be updated by it so that it can replace the regular fade behaviour.
        /// </summary>
        protected void Apply(AnimancerNode node)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the desired weight for the target state at the specified `progress` (ranging from 0 to 1).
        /// </summary>
        /// <remarks>
        /// This method should return 0 when the `progress` is 0 and 1 when the `progress` is 1. It can do anything you
        /// want with other values, but violating that guideline will trigger
        /// <see cref="OptionalWarning.CustomFadeBounds"/>.
        /// </remarks>
        protected abstract float CalculateWeight(float progress);

        /// <summary>Called when this fade is cancelled (or ends).</summary>
        /// <remarks>Can be used to return it to an <see cref="ObjectPool"/>.</remarks>
        protected abstract void Release();

        /************************************************************************************************************************/

        void IUpdatable.Update()
        {
        }

        /************************************************************************************************************************/

        private static void ForceFinishFade(AnimancerNode node)
        {
        }

        /************************************************************************************************************************/
    }
}
