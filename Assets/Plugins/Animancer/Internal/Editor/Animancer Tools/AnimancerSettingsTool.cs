// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using Object = UnityEngine.Object;

namespace Animancer.Editor.Tools
{
    /// <summary>[Editor-Only] Displays the <see cref="AnimancerSettings"/>.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.Tools/AnimancerSettingsTool
    [Serializable]
    internal class AnimancerSettingsTool : AnimancerToolsWindow.Tool
    {
        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override int DisplayOrder => int.MaxValue;

        /// <inheritdoc/>
        public override string Name => "Settings";

        /// <inheritdoc/>
        public override string Instructions => null;

        /// <inheritdoc/>
        public override string HelpURL => Strings.DocsURLs.APIDocumentation + "." + nameof(Editor) + "/" + nameof(AnimancerSettings);

        /************************************************************************************************************************/

        [NonSerialized]
        private UnityEditor.Editor _SettingsEditor;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnEnable(int index)
        {
        }

        /// <inheritdoc/>
        public override void OnDisable()
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void DoBodyGUI()
        {
        }

        /************************************************************************************************************************/
    }
}

#endif

