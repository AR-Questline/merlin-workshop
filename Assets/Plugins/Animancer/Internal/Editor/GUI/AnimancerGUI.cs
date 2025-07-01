// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] Various GUI utilities used throughout Animancer.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/AnimancerGUI
    /// 
    public static class AnimancerGUI
    {
        /************************************************************************************************************************/
        #region Standard Values
        /************************************************************************************************************************/

        /// <summary>The highlight color used for fields showing a warning.</summary>
        public static readonly Color
            WarningFieldColor = new Color(1, 0.9f, 0.6f);

        /// <summary>The highlight color used for fields showing an error.</summary>
        public static readonly Color
            ErrorFieldColor = new Color(1, 0.6f, 0.6f);

        /************************************************************************************************************************/

        /// <summary><see cref="GUILayout.ExpandWidth"/> set to false.</summary>
        public static readonly GUILayoutOption[]
            DontExpandWidth = { GUILayout.ExpandWidth(false) };

        /************************************************************************************************************************/

        /// <summary>Returns <see cref="EditorGUIUtility.singleLineHeight"/>.</summary>
        public static float LineHeight => EditorGUIUtility.singleLineHeight;

        /************************************************************************************************************************/

        /// <summary>Returns <see cref="EditorGUIUtility.standardVerticalSpacing"/>.</summary>
        public static float StandardSpacing => EditorGUIUtility.standardVerticalSpacing;

        /************************************************************************************************************************/

        private static float _IndentSize = -1;

        /// <summary>
        /// The number of pixels of indentation for each <see cref="EditorGUI.indentLevel"/> increment.
        /// </summary>
        public static float IndentSize
        {
            get
            {
                if (_IndentSize < 0)
                {
                    var indentLevel = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 1;
                    _IndentSize = EditorGUI.IndentedRect(new Rect()).x;
                    EditorGUI.indentLevel = indentLevel;
                }

                return _IndentSize;
            }
        }

        /************************************************************************************************************************/

        private static float _ToggleWidth = -1;

        /// <summary>The width of a standard <see cref="GUISkin.toggle"/> with no label.</summary>
        public static float ToggleWidth
        {
            get
            {
                if (_ToggleWidth == -1)
                    _ToggleWidth = GUI.skin.toggle.CalculateWidth(GUIContent.none);
                return _ToggleWidth;
            }
        }

        /************************************************************************************************************************/

        /// <summary>The color of the standard label text.</summary>
        public static Color TextColor => GUI.skin.label.normal.textColor;

        /************************************************************************************************************************/

        private static GUIStyle _MiniButton;

        /// <summary>A more compact <see cref="EditorStyles.miniButton"/> with a fixed size as a tiny box.</summary>
        public static GUIStyle MiniButton
        {
            get
            {
                if (_MiniButton == null)
                {
                    _MiniButton = new GUIStyle(EditorStyles.miniButton)
                    {
                        margin = new RectOffset(0, 0, 2, 0),
                        padding = new RectOffset(2, 3, 2, 2),
                        alignment = TextAnchor.MiddleCenter,
                        fixedHeight = LineHeight,
                        fixedWidth = LineHeight - 1
                    };
                }

                return _MiniButton;
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Layout
        /************************************************************************************************************************/

        /// <summary>Calls <see cref="UnityEditorInternal.InternalEditorUtility.RepaintAllViews"/>.</summary>
        public static void RepaintEverything()
            => UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

        /************************************************************************************************************************/

        /// <summary>Indicates where <see cref="LayoutSingleLineRect"/> should add the <see cref="StandardSpacing"/>.</summary>
        public enum SpacingMode
        {
            /// <summary>No extra space.</summary>
            None,

            /// <summary>Add extra space before the new area.</summary>
            Before,

            /// <summary>Add extra space after the new area.</summary>
            After,

            /// <summary>Add extra space before and after the new area.</summary>
            BeforeAndAfter
        }

        /// <summary>
        /// Uses <see cref="GUILayoutUtility.GetRect(float, float)"/> to get a <see cref="Rect"/> occupying a single
        /// standard line with the <see cref="StandardSpacing"/> added according to the specified `spacing`.
        /// </summary>
        public static Rect LayoutSingleLineRect(SpacingMode spacing = SpacingMode.None)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If the <see cref="Rect.height"/> is positive, this method moves the <see cref="Rect.y"/> by that amount and
        /// adds the <see cref="EditorGUIUtility.standardVerticalSpacing"/>.
        /// </summary>
        public static void NextVerticalArea(ref Rect area)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Subtracts the `width` from the left side of the `area` and returns a new <see cref="Rect"/> occupying the
        /// removed section.
        /// </summary>
        public static Rect StealFromLeft(ref Rect area, float width, float padding = 0)
        {
            return default;
        }

        /// <summary>
        /// Subtracts the `width` from the right side of the `area` and returns a new <see cref="Rect"/> occupying the
        /// removed section.
        /// </summary>
        public static Rect StealFromRight(ref Rect area, float width, float padding = 0)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Divides the given `area` such that the fields associated with both labels will have equal space
        /// remaining after the labels themselves.
        /// </summary>
        public static void SplitHorizontally(Rect area, string label0, string label1,
             out float width0, out float width1, out Rect rect0, out Rect rect1)
        {
            width0 = default(float);
            width1 = default(float);
            rect0 = default(Rect);
            rect1 = default(Rect);
        }

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension] Calls <see cref="GUIStyle.CalcMinMaxWidth"/> and returns the max width.</summary>
        public static float CalculateWidth(this GUIStyle style, GUIContent content)
        {
            return default;
        }

        /// <summary>[Animancer Extension] Calls <see cref="GUIStyle.CalcMinMaxWidth"/> and returns the max width.</summary>
        public static float CalculateWidth(this GUIStyle style, string text)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a <see cref="ConversionCache{TKey, TValue}"/> for calculating the GUI width occupied by text using the
        /// specified `style`.
        /// </summary>
        public static ConversionCache<string, float> CreateWidthCache(GUIStyle style)
            => new ConversionCache<string, float>((text) => style.CalculateWidth(text));

        /************************************************************************************************************************/

        private static ConversionCache<string, float> _LabelWidthCache;

        /// <summary>
        /// Calls <see cref="GUIStyle.CalcMinMaxWidth"/> using <see cref="GUISkin.label"/> and returns the max
        /// width. The result is cached for efficient reuse.
        /// </summary>
        public static float CalculateLabelWidth(string text)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Begins a vertical layout group using the given style and decreases the
        /// <see cref="EditorGUIUtility.labelWidth"/> to compensate for the indentation.
        /// </summary>
        public static void BeginVerticalBox(GUIStyle style)
        {
        }

        /// <summary>
        /// Ends a layout group started by <see cref="BeginVerticalBox"/> and restores the
        /// <see cref="EditorGUIUtility.labelWidth"/>.
        /// </summary>
        public static void EndVerticalBox(GUIStyle style)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Clears the <see cref="Selection.objects"/> then returns it to its current state.</summary>
        /// <remarks>
        /// This forces the <see cref="UnityEditorInternal.ReorderableList"/> drawer to adjust to height changes which
        /// it unfortunately doesn't do on its own..
        /// </remarks>
        public static void ReSelectCurrentObjects()
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Labels
        /************************************************************************************************************************/

        private static GUIStyle _WeightLabelStyle;
        private static float _WeightLabelWidth = -1;

        /// <summary>
        /// Draws a label showing the `weight` aligned to the right side of the `area` and reduces its
        /// <see cref="Rect.width"/> to remove that label from its area.
        /// </summary>
        public static void DoWeightLabel(ref Rect area, float weight)
        {
        }

        /************************************************************************************************************************/

        private static ConversionCache<float, string> _ShortWeightCache;

        /// <summary>Returns a string which approximates the `weight` into no more than 3 digits.</summary>
        private static string WeightToShortString(float weight, out bool isExact)
        {
            isExact = default(bool);
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>The <see cref="EditorGUIUtility.labelWidth"/> from before <see cref="BeginTightLabel"/>.</summary>
        private static float _TightLabelWidth;

        /// <summary>Stores the <see cref="EditorGUIUtility.labelWidth"/> and changes it to the exact width of the `label`.</summary>
        public static string BeginTightLabel(string label)
        {
            return default;
        }

        /// <summary>Reverts <see cref="EditorGUIUtility.labelWidth"/> to its previous value.</summary>
        public static void EndTightLabel()
        {
        }

        /************************************************************************************************************************/

        private static ConversionCache<string, string> _NarrowTextCache;

        /// <summary>
        /// Returns the `text` without any spaces if <see cref="EditorGUIUtility.wideMode"/> is false.
        /// Otherwise simply returns the `text` without any changes.
        /// </summary>
        public static string GetNarrowText(string text)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Loads an icon texture and sets it to use <see cref="FilterMode.Bilinear"/>.</summary>
        public static Texture LoadIcon(string name)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Calls <see cref="EditorGUIUtility.IconContent(string)"/> if the `content` was null.</summary>
        public static GUIContent IconContent(ref GUIContent content, string name)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Draws a button using <see cref="EditorStyles.miniButton"/> and <see cref="DontExpandWidth"/>.</summary>
        public static bool CompactMiniButton(GUIContent content)
            => GUILayout.Button(content, EditorStyles.miniButton, DontExpandWidth);

        /// <summary>Draws a button using <see cref="EditorStyles.miniButton"/>.</summary>
        public static bool CompactMiniButton(Rect area, GUIContent content)
            => GUI.Button(area, content, EditorStyles.miniButton);

        /************************************************************************************************************************/

        private static GUIContent
            _PlayButtonContent,
            _PauseButtonContent,
            _StepBackwardButtonContent,
            _StepForwardButtonContent;

        /// <summary><see cref="IconContent(ref GUIContent, string)"/> for a play button.</summary>
        public static GUIContent PlayButtonContent
            => IconContent(ref _PlayButtonContent, "PlayButton");

        /// <summary><see cref="IconContent(ref GUIContent, string)"/> for a pause button.</summary>
        public static GUIContent PauseButtonContent
            => IconContent(ref _PauseButtonContent, "PauseButton");

        /// <summary><see cref="IconContent(ref GUIContent, string)"/> for a step backward button.</summary>
        public static GUIContent StepBackwardButtonContent
            => IconContent(ref _StepBackwardButtonContent, "Animation.PrevKey");

        /// <summary><see cref="IconContent(ref GUIContent, string)"/> for a step forward button.</summary>
        public static GUIContent StepForwardButtonContent
            => IconContent(ref _StepForwardButtonContent, "Animation.NextKey");

        /************************************************************************************************************************/

        private static float _PlayButtonWidth;

        /// <summary>The default width of <see cref="PlayButtonContent"/> using <see cref="EditorStyles.miniButton"/>.</summary>
        public static float PlayButtonWidth
        {
            get
            {
                if (_PlayButtonWidth <= 0)
                    EditorStyles.miniButton.CalcMinMaxWidth(PlayButtonContent, out _PlayButtonWidth, out _);
                return _PlayButtonWidth;
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Events
        /************************************************************************************************************************/

        /// <summary>
        /// Returns true and uses the current event if it is <see cref="EventType.MouseUp"/> inside the specified
        /// `area`.
        /// </summary>
        public static bool TryUseClickEvent(Rect area, int button = -1)
        {
            return default;
        }

        /// <summary>
        /// Returns true and uses the current event if it is <see cref="EventType.MouseUp"/> inside the last GUI Layout
        /// <see cref="Rect"/> that was drawn.
        /// </summary>
        public static bool TryUseClickEventInLastRect(int button = -1)
            => TryUseClickEvent(GUILayoutUtility.GetLastRect(), button);

        /************************************************************************************************************************/

        /// <summary>
        /// Invokes `onDrop` if the <see cref="Event.current"/> is a drag and drop event inside the `dropArea`.
        /// </summary>
        public static void HandleDragAndDrop<T>(Rect dropArea, Func<T, bool> validate, Action<T> onDrop,
            DragAndDropVisualMode mode = DragAndDropVisualMode.Link) where T : class
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Updates the <see cref="DragAndDrop.visualMode"/> or calls `onDrop` for each of the `objects`.
        /// </summary>
        private static void TryDrop<T>(IEnumerable objects, Func<T, bool> validate, Action<T> onDrop, bool isDrop,
            DragAndDropVisualMode mode) where T : class
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Uses <see cref="HandleDragAndDrop"/> to deal with drag and drop operations involving
        /// <see cref="AnimationClip"/>s of <see cref="IAnimationClipSource"/>s.
        /// </summary>
        public static void HandleDragAndDropAnimations(Rect dropArea, Action<AnimationClip> onDrop,
            DragAndDropVisualMode mode = DragAndDropVisualMode.Link)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Deselects any selected IMGUI control.</summary>
        public static void Deselect() => GUIUtility.keyboardControl = 0;

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

