// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using Animancer.Editor.Tools;
using Animancer.Units;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only]
    /// A custom Inspector for <see cref="Sprite"/>s which allows you to directly edit them instead of just showing
    /// their details like the default one does.
    /// </summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/SpriteEditor
    /// 
    [CustomEditor(typeof(Sprite), true), CanEditMultipleObjects]
    public class SpriteEditor : UnityEditor.Editor
    {
        /************************************************************************************************************************/

        private const string
            NameTooltip = "The asset name of the sprite",
            RectTooltip = "The texture area occupied by the sprite",
            PivotTooltip = "The origin point of the sprite relative to its Rect",
            BorderTooltip = "The edge sizes used when 9-Slicing the sprite for the UI system (ignored by SpriteRenderers)";

        [NonSerialized]
        private SerializedProperty
            _Name,
            _Rect,
            _Pivot,
            _Border;

        [NonSerialized]
        private NormalizedPixelField[]
            _RectFields,
            _PivotFields,
            _BorderFields;

        [NonSerialized]
        private bool _HasBeenModified;

        [NonSerialized]
        private Target[] _Targets;

        private readonly struct Target
        {
            public readonly Sprite Sprite;
            public readonly string AssetPath;
            public readonly TextureImporter Importer;

            public Target(Object target) : this()
            {
            }
        }

        /************************************************************************************************************************/

        /// <summary>Initializes this editor.</summary>
        protected virtual void OnEnable()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Cleans up this editor.</summary>
        protected virtual void OnDisable()
        {
        }

        /************************************************************************************************************************/
        #region Inspector
        /************************************************************************************************************************/

        /// <summary>Are all targets set to <see cref="SpriteImportMode.Multiple"/>?</summary>
        private bool AllSpriteModeMultiple
        {
            get
            {
                for (int i = 0; i < _Targets.Length; i++)
                {
                    var importer = _Targets[i].Importer;
                    if (importer == null ||
                        importer.spriteImportMode != SpriteImportMode.Multiple)
                        return false;
                }

                return true;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Called by the Unity editor to draw the custom Inspector GUI elements.</summary>
        public override void OnInspectorGUI()
        {
        }

        /************************************************************************************************************************/

        private void DoNameGUI()
        {
        }

        /************************************************************************************************************************/

        private void DoRectGUI()
        {
        }

        /************************************************************************************************************************/

        private void DoPivotGUI()
        {
        }

        /************************************************************************************************************************/

        private void DoBorderGUI()
        {
        }

        /************************************************************************************************************************/

        private void Revert()
        {
        }

        /************************************************************************************************************************/

        private void Apply()
        {
        }

        /************************************************************************************************************************/

        private void Apply(SpriteDataEditor data, Sprite sprite, ref bool hasError)
        {
        }

        /************************************************************************************************************************/
        #region Normalized Pixel Field
        /************************************************************************************************************************/

        /// <summary>
        /// A wrapper around a <see cref="SerializedProperty"/> to display it using two float fields where one is
        /// normalized and the other is not.
        /// </summary>
        private class NormalizedPixelField
        {
            /************************************************************************************************************************/

            /// <summary>The target property.</summary>
            public readonly SerializedProperty Property;

            /// <summary>The label to display next to the property.</summary>
            public readonly GUIContent Label;

            /// <summary>Is the serialized property value normalized?</summary>
            public readonly bool IsNormalized;

            /// <summary>The multiplier to turn a non-normalized value into a normalized one.</summary>
            public float normalizeMultiplier;

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="NormalizedPixelField"/>.</summary>
            public NormalizedPixelField(SerializedProperty property, GUIContent label, bool isNormalized)
            {
            }

            /************************************************************************************************************************/

            /// <summary>Draws a group of <see cref="NormalizedPixelField"/>s.</summary>
            public static void DoGroupGUI(SerializedProperty baseProperty, GUIContent label, NormalizedPixelField[] fields)
            {
            }

            /************************************************************************************************************************/

            /// <summary>Draws this <see cref="NormalizedPixelField"/>.</summary>
            public void DoTwinFloatFieldGUI(Rect area)
            {
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Normalized Pixel Field Attribute
        /************************************************************************************************************************/

        private class NormalizedPixelFieldAttribute : UnitsAttribute
        {
            /************************************************************************************************************************/

            private static new readonly float[] Multipliers = new float[2];

            public void CalculateMultipliers(float normalizeMultiplier)
            {
            }

            /************************************************************************************************************************/

            private static new readonly CompactUnitConversionCache[] DisplayConverters =
            {
                new CompactUnitConversionCache("px"),
                AnimationTimeAttribute.XSuffix,
            };

            /************************************************************************************************************************/

            public static readonly NormalizedPixelFieldAttribute Pixel = new NormalizedPixelFieldAttribute(false);
            public static readonly NormalizedPixelFieldAttribute Normalized = new NormalizedPixelFieldAttribute(true);

            /************************************************************************************************************************/

            public NormalizedPixelFieldAttribute(bool isNormalized)
            {
            }

            /************************************************************************************************************************/

            /// <inheritdoc/>
            protected override int GetLineCount(SerializedProperty property, GUIContent label) => 1;

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Preview
        /************************************************************************************************************************/

        private static readonly Type
            DefaultEditorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.SpriteInspector");

        private readonly Dictionary<Object, UnityEditor.Editor>
            TargetToDefaultEditor = new Dictionary<Object, UnityEditor.Editor>();

        /************************************************************************************************************************/

        private void InitializePreview()
        {
        }

        /************************************************************************************************************************/

        private void CleanUpPreview()
        {
        }

        /************************************************************************************************************************/

        private bool TryGetDefaultEditor(out UnityEditor.Editor editor)
            => TargetToDefaultEditor.TryGetValue(target, out editor);

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override string GetInfoString()
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
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
        public override void OnPreviewGUI(Rect area, GUIStyle background)
        {
        }

        /************************************************************************************************************************/

        private static void FitAspectRatio(ref Rect area, Sprite sprite)
        {
        }

        /************************************************************************************************************************/

        private static readonly int PivotDotControlIDHint = "PivotDot".GetHashCode();

        private static GUIStyle _PivotDot;
        private static GUIStyle _PivotDotActive;

        [NonSerialized] private Vector2 _MouseDownPivot;

        private void DoPivotDotGUI(Rect area, Sprite sprite)
        {
        }

        /************************************************************************************************************************/

        /// <summary>The opposite of <see cref="Mathf.LerpUnclamped(float, float, float)"/>.</summary>
        public static float InverseLerpUnclamped(float a, float b, float value)
        {
            return default;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

