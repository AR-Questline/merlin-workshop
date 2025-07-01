// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Animancer.Editor.Tools
{
    /// <summary>[Editor-Only] [Pro-Only] 
    /// A <see cref="AnimancerToolsWindow.Tool"/> for changing which bones an <see cref="AnimationClip"/>s controls.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/tools/remap-animation-bindings">Remap Animation Bindings</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.Tools/RemapAnimationBindingsTool
    /// 
    [Serializable]
    public class RemapAnimationBindingsTool : AnimationModifierTool
    {
        /************************************************************************************************************************/

        [SerializeField] private List<string> _NewBindingPaths;

        [NonSerialized] private List<List<EditorCurveBinding>> _BindingGroups;
        [NonSerialized] private List<string> _OldBindingPaths;
        [NonSerialized] private bool _OldBindingPathsAreDirty;
        [NonSerialized] private ReorderableList _OldBindingPathsDisplay;
        [NonSerialized] private ReorderableList _NewBindingPathsDisplay;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override int DisplayOrder => 5;

        /// <inheritdoc/>
        public override string Name => "Remap Animation Bindings";

        /// <inheritdoc/>
        public override string HelpURL => Strings.DocsURLs.RemapAnimationBindings;

        /// <inheritdoc/>
        public override string Instructions
        {
            get
            {
                if (Animation == null)
                    return "Select the animation you want to remap.";

                if (_OldBindingPaths.Count == 0)
                {
                    if (Animation.humanMotion)
                        return "The selected animation only has Humanoid bindings which cannot be remapped.";

                    return "The selected animation does not have any bindings.";
                }

                return "Enter the new paths to change the bindings into then click Save As.";
            }
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnEnable(int index)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void OnAnimationChanged()
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void DoBodyGUI()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Gathers the bindings from the <see cref="AnimationModifierTool.Animation"/>.</summary>
        private void GatherBindings()
        {
        }

        /************************************************************************************************************************/

        private static HashSet<string> _HumanoidBindingNames;

        /// <summary>Is the `propertyName` one of the bindings used by Humanoid animations?</summary>
        private static bool IsHumanoidBinding(string propertyName)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Copies all of the <see cref="_NewBindingPaths"/> to the system clipboard.</summary>
        private void CopyAll()
        {
        }

        /// <summary>Pastes the string from the system clipboard into the <see cref="_NewBindingPaths"/>.</summary>
        private void PasteAll()
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void Modify(AnimationClip animation)
        {
        }

        /************************************************************************************************************************/
    }
}

#endif

