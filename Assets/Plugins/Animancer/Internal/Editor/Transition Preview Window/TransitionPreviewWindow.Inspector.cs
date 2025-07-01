// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animancer.Editor
{
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/TransitionPreviewWindow
    partial class TransitionPreviewWindow
    {
        /// <summary>[Internal] Custom Inspector for the <see cref="TransitionPreviewWindow"/>.</summary>
        /// <remarks>
        /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/transitions#previews">Previews</see>
        /// </remarks>
        [CustomEditor(typeof(TransitionPreviewWindow))]
        internal class Inspector : UnityEditor.Editor
        {
            /************************************************************************************************************************/

            private static readonly string[]
                TabNames = { "Preview", "Settings" };

            private const int
                PreviewTab = 0,
                SettingsTab = 1;

            /************************************************************************************************************************/

            [SerializeField]
            private int _CurrentTab;

            private readonly AnimancerPlayableDrawer
                PlayableDrawer = new AnimancerPlayableDrawer();

            public TransitionPreviewWindow Target { get; private set; }

            /************************************************************************************************************************/

            public override bool UseDefaultMargins() => false;

            /************************************************************************************************************************/

            public override void OnInspectorGUI()
            {
            }

            /************************************************************************************************************************/

            private void DoPreviewInspectorGUI()
            {
            }

            /************************************************************************************************************************/

            /// <summary>The tooltip used for the Close button.</summary>
            public const string CloseTooltip = "Close the Transition Preview Window";

            /// <summary>Draws the target object and path of the <see cref="TransitionProperty"/>.</summary>
            private void DoTransitionPropertyGUI()
            {
            }

            /************************************************************************************************************************/

            private void DoTransitionGUI()
            {
            }

            /************************************************************************************************************************/
        }
    }
}

#endif

