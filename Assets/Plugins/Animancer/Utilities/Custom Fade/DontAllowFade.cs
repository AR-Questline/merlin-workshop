// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using UnityEngine;

namespace Animancer
{
    /// <summary>An <see cref="IUpdatable"/> that cancels any fades and logs warnings when they occur.</summary>
    /// 
    /// <remarks>
    /// This is useful for <see cref="Sprite"/> based characters since fading does nothing for them.
    /// <para></para>
    /// You can also set the <see cref="AnimancerPlayable.DefaultFadeDuration"/> to 0 so that you don't need to set it
    /// manually on all your transitions.
    /// </remarks>
    /// 
    /// <example><code>
    /// [SerializeField] private AnimancerComponent _Animancer;
    /// 
    /// private void Awake()
    /// {
    ///     // To only apply it only in the Unity Editor and Development Builds:
    ///     DontAllowFade.Assert(_Animancer);
    ///     
    ///     // Or to apply it at all times:
    ///     _Animancer.Playable.RequireUpdate(new DontAllowFade());
    /// }
    /// </code></example>
    /// 
    /// https://kybernetik.com.au/animancer/api/Animancer/DontAllowFade
    /// 
    public class DontAllowFade : Key, IUpdatable
    {
        /************************************************************************************************************************/

        /// <summary>[Assert-Conditional] Applies a <see cref="DontAllowFade"/> to `animancer`.</summary>
        [System.Diagnostics.Conditional(Strings.Assertions)]
        public static void Assert(AnimancerPlayable animancer)
        {
        }

        /************************************************************************************************************************/

        /// <summary>If the `node` is fading, this methods logs a warning (Assert-Only) and cancels the fade.</summary>
        private static void Validate(AnimancerNode node)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Calls <see cref="Validate"/> on all layers and their <see cref="AnimancerLayer.CurrentState"/>.</summary>
        void IUpdatable.Update()
        {
        }

        /************************************************************************************************************************/
    }
}
