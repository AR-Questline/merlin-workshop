// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] [Pro-Only] A custom Inspector for <see cref="AnimationClip"/>s</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/AnimationClipEditor
    /// 
    [CustomEditor(typeof(AnimationClip))]
    public class AnimationClipEditor : UnityEditor.Editor
    {
        /************************************************************************************************************************/

        private const string DefaultEditorTypeName = nameof(UnityEditor) + "." + nameof(AnimationClipEditor);

        private static readonly Type
            DefaultEditorType = typeof(UnityEditor.Editor).Assembly.GetType(DefaultEditorTypeName);

        /************************************************************************************************************************/

        private UnityEditor.Editor _DefaultEditor;

        private bool TryGetDefaultEditor(out UnityEditor.Editor editor)
        {
            editor = default(UnityEditor.Editor);
            return default;
        }

        /************************************************************************************************************************/

        protected virtual void OnDestroy()
        {
        }

        /************************************************************************************************************************/

        private static HashSet<Object> _DestroyOnPlayModeStateChanged;

        private static void DestroyOnPlayModeStateChanged(Object obj)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Draws the regular Inspector then adds a better preview for <see cref="Sprite"/> animations.</summary>
        /// <remarks>Called by the Unity editor to draw the custom Inspector GUI elements.</remarks>
        public override void OnInspectorGUI()
        {
        }

        /************************************************************************************************************************/

        private AnimationClip GetTargetClip(out AnimationType type)
        {
            type = default(AnimationType);
            return default;
        }

        /************************************************************************************************************************/

        [SerializeField]
        private bool _ShowEvents = true;

        private void DrawEvents(AnimationClip clip)
        {
        }

        /************************************************************************************************************************/

        [NonSerialized]
        private bool _HasInitializedSpritePreview;

        private void InitializeSpritePreview(UnityEditor.Editor editor, AnimationClip clip)
        {
        }

        /************************************************************************************************************************/

        private static ConversionCache<int, string> _FrameCache;
        private static ConversionCache<float, string> _TimeCache;

        private static void DrawSpriteFrames(AnimationClip clip)
        {
        }

        /************************************************************************************************************************/

        private static ObjectReferenceKeyframe[] GetSpriteReferences(AnimationClip clip)
        {
            return default;
        }

        /************************************************************************************************************************/
        #region Redirects
        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void DrawPreview(Rect previewArea)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override string GetInfoString()
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override GUIContent GetPreviewTitle()
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override bool HasPreviewGUI()
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnInteractivePreviewGUI(Rect area, GUIStyle background)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnPreviewGUI(Rect area, GUIStyle background)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnPreviewSettings()
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void ReloadPreviewInstances()
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override bool RequiresConstantRepaint()
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override bool UseDefaultMargins()
        {
            return default;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

