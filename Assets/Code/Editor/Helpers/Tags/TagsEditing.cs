using Awaken.TG.Main.Utility.Tags;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Utility.Attributes.Tags;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Path = System.IO.Path;

namespace Awaken.TG.Editor.Helpers.Tags {
    public class TagsEditing {

        // === Factory
        static readonly Dictionary<string, TagsEditing> Property2Drawer = new();

        static string KeyFor(SerializedProperty property) {
            return $"{property.serializedObject.targetObject.GetInstanceID()}{property.name}{property.propertyPath}";
        }

        /// <summary>
        /// Here are tags properties drawn
        /// </summary>
        public static void Show(SerializedProperty property, TagsCategory tagsCategory, int? overrideWidth = null) {
            if (property == null) {
                // Happens when the property is not serialized
                EditorGUILayout.HelpBox("Tags property is null, make sure the field is serialized", MessageType.Error);
                return;
            }
            if (property.hasMultipleDifferentValues) {
                EditorGUILayout.HelpBox("Different values present. Cannot edit Tags", MessageType.Error);
                return;
            }

            string key = KeyFor(property);
            if (!Property2Drawer.TryGetValue(key, out TagsEditing drawer)) {
                drawer = new TagsEditing(tagsCategory, overrideWidth);
                Property2Drawer.Add(key, drawer);
            }
            drawer.Show(property);
        }

        public static void ResetDrawer(SerializedProperty property) {
            string key = KeyFor(property);
            if (Property2Drawer.ContainsKey(key)) {
                Property2Drawer.Remove(key);
            }
        }

        // === const and semi-const
        const string InputNameDefault = "tagInputTextField";
        const int InputWidth = 165;
        public const char KindValueSeparator = ':';
        static readonly GUIContent ButtonGUIContent = new(" X ");
        static GUISkin s_customSkin;
        const int SearchButtonWidth = 25;

        // unique gui names
        readonly string _inputName;
        readonly string _hintPrefix;

        // fields
        readonly string _categoryName;
        readonly TagsCache _tagsCache;

        static int s_id = 0;

        // === flags and states
        string _newTagInput = string.Empty;
        bool _removeSelection;
        bool _waitForTabUp = false;
        TextEditor _textEditor = null;
        int? _overrideWidth;
        string _focusedHint;

        int HintsOffset => _overrideWidth.HasValue ? 0 : 50;

        float ViewWidth => _overrideWidth ?? EditorGUIUtility.currentViewWidth - StyleAdditionalWidth(EditorStyles.inspectorFullWidthMargins);

        private TagsEditing(TagsCategory target, int? overrideWidth = null) {
            _inputName = $"{InputNameDefault}{s_id}";
            _hintPrefix = $"{s_id}_hint_";
            _categoryName = target.ToString();
            _overrideWidth = overrideWidth;

            if (s_customSkin == null) {
                s_customSkin = Resources.Load<GUISkin>("Data/Tags/TagsEditorSkin");
            }

            _tagsCache = TagsCache.Get(target);

            s_id++;
        }

        public void Show(SerializedProperty property) {
            if (!IsValidProperty(property)) {
                EditorGUILayout.HelpBox(property.name + " is neither array of strings nor single string", MessageType.Error);
                return;
            }

            // deselect input text after tab
            TryRemoveSelectionFromInput();

            // get tags list
            List<string> tags = GetSerializedTags(property);

            // get hints trimmed to windows width
            var hints = Hints(tags)?.ToList() ?? new List<string>();
            TrimHints(hints);

            // drawing
            DrawPrefixLabel(property);
            string newTag = string.Empty;
            // draw input if editing array or first tag
            if (IsSemanticArray(property) || tags.Count < 1) {
                newTag = DrawInput(property, hints, tags);
            }
            int removedTag = DrawTags(tags);
            UpdateHintsSelection(hints);
            
            //if (Event.current.type == EventType.KeyDown && GUI.GetNameOfFocusedControl().Contains(_inputName)) {
            //    GUI.FocusControl($"{_hintPrefix}0");
            //}

            // process tags
            UpdateTags(newTag, tags, removedTag);
            UpdateTagProperty(property, tags);
        }

        // === drawing
        void DrawPrefixLabel(SerializedProperty tagsProperty) {
            var noLabelAttribute = tagsProperty.ExtractAttribute<HideLabelAttribute>();
            if (noLabelAttribute == null) {
                EditorGUILayout.LabelField($"{tagsProperty.displayName} ({_categoryName})");
            }
        }

        string DrawInput(SerializedProperty property, List<string> hints, List<string> tags) {
            EditorGUILayout.BeginHorizontal();
            GUI.SetNextControlName(_inputName);
            _newTagInput = GUILayout.TextField(_newTagInput, s_customSkin.textField, GUILayout.Width(InputWidth));

            // get reference for deselection
            if (_removeSelection && GUI.GetNameOfFocusedControl().Equals(_inputName)) {
                _textEditor = (TextEditor) GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            }

            // enter up and input is focused
            if (Event.current.type == EventType.KeyUp && Event.current.keyCode is KeyCode.Return or KeyCode.KeypadEnter && GUI.GetNameOfFocusedControl().Equals(_inputName)) {
                EditorGUILayout.EndHorizontal();
                FocusInput();
                return _newTagInput;
            }

            DrawSearch(property, tags);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            var hintsOutput = DrawHints(hints);
            EditorGUILayout.EndHorizontal();

            return hintsOutput;
        }

        void DrawSearch(SerializedProperty property, List<string> tags) {
            // --- dropdown
            Texture browseIcon = EditorGUIUtility.Load("FMOD/SearchIconBlack.png") as Texture;
            GUIStyle buttonStyle = new(GUI.skin.button) {
                padding = {
                    top = 1,
                    bottom = 1
                }
            };
            if (GUILayout.Button(new GUIContent(browseIcon, "Search"), buttonStyle, GUILayout.Width(SearchButtonWidth))) {
                TagsBrowser eventBrowser = ScriptableObject.CreateInstance<TagsBrowser>();
                eventBrowser.Show(property, _tagsCache, tags);
                Rect windowRect = GUILayoutUtility.GetLastRect();
                windowRect.position = GUIUtility.GUIToScreenPoint(windowRect.position);
                eventBrowser.ShowAsDropDown(windowRect, new Vector2(200, 300));
            }
        }
        
        string DrawHints(List<string> hints) {
            for (int index = 0; index < hints.Count; index++) {
                string hint = hints[index];
                bool writingValue = _newTagInput.Contains(KindValueSeparator);
                string tooltip;
                if (writingValue) {
                    tooltip = _tagsCache.TryFindEntry(TagUtils.TagKind(_newTagInput), out var entry) && entry.TryFindValue(hint, out var value)
                        ? value.context
                        : null;
                } else {
                    tooltip = _tagsCache.TryFindEntry(hint, out var entry)
                        ? entry.kind.context
                        : null;
                }
                GUI.SetNextControlName($"{_hintPrefix}{index}");
                
                if (GUILayout.Button(new GUIContent(hint, tooltip), s_customSkin.label)) {
                    FocusInput();

                    if (!writingValue) {
                        _newTagInput = hint + KindValueSeparator;
                        return null;
                    }

                    _newTagInput = TagUtils.TagKind(_newTagInput) + KindValueSeparator + hint;
                    return _newTagInput;
                }
            }

            return null;
        }

        int DrawTags(List<string> tags) {
            int removedTag = -1;
            // calculate screen widths
            float horizontalSpace = ViewWidth;
            var buttonContentSize = s_customSkin.button.CalcSize(ButtonGUIContent);
            float tagWidthWithoutContent = StyleAdditionalWidth(s_customSkin.textArea) + StyleAdditionalWidth(s_customSkin.button) + buttonContentSize.x +
                                           StyleAdditionalWidth(s_customSkin.box);

            // begin row
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();

            for (int i = 0; i < tags.Count; i++) {
                string tag = tags[i];
                string tooltip = _tagsCache.GetContext(tag);

                var tagContent = new GUIContent(tag, tooltip);

                var tagContentSize = s_customSkin.textArea.CalcSize(tagContent);
                float tagWidth = tagContentSize.x + tagWidthWithoutContent;
                horizontalSpace -= tagWidth;

                // end row and start new one
                if (horizontalSpace < 30) {
                    horizontalSpace = ViewWidth - tagWidth;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }

                bool remove = DrawTag(tagContent, tagContentSize.x);
                // removed
                if (remove) {
                    removedTag = i;
                    FocusInput();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return removedTag;
        }

        bool DrawTag(GUIContent content, float width) {
            EditorGUILayout.BeginHorizontal(s_customSkin.box);

            GUILayout.Label(content, s_customSkin.textArea, GUILayout.Width(width));

            bool removeThis = GUILayout.Button("X", s_customSkin.button, GUILayout.Width(20), GUILayout.Height(20));

            EditorGUILayout.EndHorizontal();

            return removeThis;
        }

        // === logic

        List<string> GetSerializedTags(SerializedProperty property) {
            List<string> tags = new();
            
            if (IsSemanticArray(property)) {
                for (int i = 0; i < property.arraySize; i++) {
                    tags.Add(property.GetArrayElementAtIndex(i).stringValue);
                }
            } else {
                tags.Add(property.stringValue);
            }

            return tags.Distinct().Where(TagUtils.IsValidTag).ToList();
        }

        IEnumerable<string> Hints(List<string> tags) {
            IEnumerable<string> hints;

            if (!_newTagInput.Contains(KindValueSeparator)) {
                hints = TagsCacheUtils.KindHints(_tagsCache, tags, _newTagInput);
            } else {
                TagUtils.Split(_newTagInput, out var tagKind, out var tagValue);
                hints = TagsCacheUtils.ValueHints(_tagsCache, tags, tagKind, tagValue);
            }

            return hints;
        }

        void TrimHints(List<string> hints) {
            var hintAdditionalWidth = StyleAdditionalWidth(s_customSkin.label);

            float editorWidth = StyleAdditionalWidth(EditorStyles.inspectorFullWidthMargins) + StyleAdditionalWidth(s_customSkin.textField);
            float horizontalSpace = ViewWidth - editorWidth - HintsOffset - SearchButtonWidth * 2f;

            int i = 0;
            for (; i < hints.Count; i++) {
                string hint = hints[i];
                var hintContent = new GUIContent(hint);
                var hintSize = s_customSkin.label.CalcSize(hintContent);
                horizontalSpace -= hintSize.x + hintAdditionalWidth;

                // out of space
                if (horizontalSpace < 15) {
                    break;
                }
            }

            for (int j = hints.Count - 1; j >= Mathf.Max(i, 1); j--) {
                hints.RemoveAt(j);
            }
        }

        void UpdateTags(string newTag, List<string> tags, int removedTag) {
            if (!string.IsNullOrWhiteSpace(newTag)) {
                if (TagUtils.IsValidTag(newTag)) {
                    tags.Add(newTag);
                    _newTagInput = string.Empty;
                } else if (!newTag.Contains(KindValueSeparator)) {
                    _newTagInput += KindValueSeparator;
                }
            } else if (removedTag != -1) {
                tags.RemoveAt(removedTag);
            }
        }

        void UpdateTagProperty(SerializedProperty property, List<string> tags) {
            // try save tags
            foreach (string tag in tags) {
                SaveTag(tag);
            }

            // copy tags to property
            if (IsSemanticArray(property)) {
                if (property.arraySize != tags.Count) {
                    property.arraySize = tags.Count;
                }

                for (int i = 0; i < tags.Count; i++) {
                    if (property.GetArrayElementAtIndex(i).stringValue != tags[i]) {
                        property.GetArrayElementAtIndex(i).stringValue = tags[i];
                    }
                }
            } else {
                string targetValue = tags.Count > 0 ? tags[0] : string.Empty;
                if (property.stringValue != targetValue) {
                    property.stringValue = targetValue;
                }
            }
        }
        
        public static void SetTagProperty(SerializedProperty property, List<string> tags) {
            // copy tags to property
            if (IsSemanticArray(property)) {
                if (property.arraySize != tags.Count) {
                    property.arraySize = tags.Count;
                }

                for (int i = 0; i < tags.Count; i++) {
                    if (property.GetArrayElementAtIndex(i).stringValue != tags[i]) {
                        property.GetArrayElementAtIndex(i).stringValue = tags[i];
                    }
                }
            } else {
                string targetValue = tags.Count > 0 ? tags[0] : string.Empty;
                if (property.stringValue != targetValue) {
                    property.stringValue = targetValue;
                }
            }
        }

        void UpdateHintsSelection(List<string> hints) {
            //HACK: unity change focus between down and up so we need to cache key down
            if (Event.current.isKey && Event.current.keyCode == KeyCode.Tab) {
                if (Event.current.type == EventType.KeyDown && GUI.GetNameOfFocusedControl().Contains(_hintPrefix)) {
                    int clickedHint = int.Parse(GUI.GetNameOfFocusedControl().Replace(_hintPrefix, ""));

                    if (clickedHint == hints.Count - 1) {
                        _waitForTabUp = true;
                    }
                } else if (Event.current.type == EventType.KeyUp && _waitForTabUp) {
                    _waitForTabUp = false;
                    FocusInput();
                }
            }
        }

        void SaveTag(string tag) {
            TagUtils.Split(tag, out var tagKind, out var tagValue);
            TagsCacheUtils.AddTag(tagKind, tagValue, _tagsCache.category);
        }

        /// <summary>
        /// Save tag to category without showing GUI
        /// </summary>
        public static void SaveTag(string tag, TagsCategory category) {
            TagUtils.Split(tag, out var tagKind, out var tagValue);
            TagsCacheUtils.AddTag(tagKind, tagValue, category);
        }

        // === utils

        public static bool IsValidProperty(SerializedProperty property) {
            return !(!(property.isArray && property.arrayElementType == "string") && (property.propertyType != SerializedPropertyType.String));
        }

        void TryRemoveSelectionFromInput() {
            // remove selection, move cursor to last position and clear flags
            if (_removeSelection && _textEditor != null && _textEditor.selectIndex > 0) {
                _textEditor.cursorIndex = _textEditor.text.Length;
                _textEditor.selectIndex = _textEditor.text.Length;
                _textEditor = null;
                _removeSelection = false;
            }
        }

        void FocusInput() {
            EditorGUI.FocusTextInControl(_inputName);
            // remove selection
            _removeSelection = true;
            // force auto repaint after change focus
            Event.current.Use();
        }

        float StyleAdditionalWidth(GUIStyle style) => (style.margin.left + style.margin.right);

        /// <summary>
        /// String property is marked as array but to us is single-value data type
        /// </summary>
        /// <returns>True if property is array but not string</returns>
        static bool IsSemanticArray(SerializedProperty property) => property.isArray && property.propertyType != SerializedPropertyType.String;
    }
}