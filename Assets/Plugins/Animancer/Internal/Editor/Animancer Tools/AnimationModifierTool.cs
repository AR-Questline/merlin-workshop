// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace Animancer.Editor.Tools
{
    /// <summary>[Editor-Only] [Pro-Only]
    /// A base <see cref="AnimancerToolsWindow.Tool"/> for modifying <see cref="AnimationClip"/>s.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/tools">Animancer Tools</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.Tools/AnimationModifierTool
    /// 
    [Serializable]
    public abstract class AnimationModifierTool : AnimancerToolsWindow.Tool
    {
        /************************************************************************************************************************/

        [SerializeField]
        private AnimationClip _Animation;

        /// <summary>The currently selected <see cref="AnimationClip"/> asset.</summary>
        public AnimationClip Animation => _Animation;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnEnable(int index)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnSelectionChanged()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Called whenever the selected <see cref="Animation"/> changes.</summary>
        protected virtual void OnAnimationChanged() {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void DoBodyGUI()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Calls <see cref="AnimancerToolsWindow.Tool.SaveModifiedAsset"/> on the animation.</summary>
        protected bool SaveAs()
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Override this to apply the desired modifications to the `animation` before it is saved.</summary>
        protected virtual void Modify(AnimationClip animation)
        {
        }

        /************************************************************************************************************************/
    }
}

#endif

