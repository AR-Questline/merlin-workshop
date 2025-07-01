using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Awaken.TG.Editor.Utility.RichLabels.Configs;
using Awaken.TG.Main.Utility.RichLabels;
using Sirenix.Utilities.Editor;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.RichLabels {
    /// <summary>
    /// Used in story graphs
    /// </summary>
    public class UsageRichLabelBrowser : RichLabelBrowser {
        const string PathToRightClickIcon = "Assets/2DAssets/UI/Generic/mouse_right.png";
        static readonly string PersistentKeyIsUsageStoryVisible = $"{nameof(UsageRichLabelBrowser)};{nameof(IsUsageHistoryVisible)}";

        List<RichLabelUsageEntry> _chosenEntries;
        Action<List<RichLabelUsageEntry>> _onChange;
        KeyCode _lastMouseKeyPressed;
        Texture _rightClickIcon;

        bool IsUsageHistoryVisible {
            get => EditorPrefs.GetBool(PersistentKeyIsUsageStoryVisible, false);
            set => EditorPrefs.SetBool(PersistentKeyIsUsageStoryVisible, value);
        }

        static Rect LabelRect => GUILayoutUtility.GetRect(ColumnWidth, ColumnWidth, ColumnItemHeight, ColumnItemHeight);

        public static void Show(Rect source, RichLabelConfig richLabelConfig, List<RichLabelUsageEntry> chosenEntries,
            Action<List<RichLabelUsageEntry>> onChange) {
            UsageRichLabelBrowser eventBrowser = ScriptableObject.CreateInstance<UsageRichLabelBrowser>();
            eventBrowser.Init(chosenEntries, onChange);
            eventBrowser.ShowWindow(source, richLabelConfig);
        }

        public override void ShowWindow(Rect source, RichLabelConfig richLabelConfig) {
            base.ShowWindow(source, richLabelConfig);
            if (_rightClickIcon == null) {
                _rightClickIcon = (Texture)AssetDatabase.LoadAssetAtPath(PathToRightClickIcon, typeof(Texture));
            }
        }

        public void Init(List<RichLabelUsageEntry> chosenEntries, Action<List<RichLabelUsageEntry>> onChange) {
            _chosenEntries = new List<RichLabelUsageEntry>(chosenEntries);
            _onChange = onChange;
        }
        
        protected override void OnImGUI() {
            if (_richLabelConfig.PersistentRichLabelUsageData.Any()) {
                GUILayout.Space(1);
                // SirenixEditorGUI.BeginHorizontalToolbar();
                // IsUsageHistoryVisible = SirenixEditorGUI.Foldout(IsUsageHistoryVisible, "Usage history");
                // SirenixEditorGUI.EndHorizontalToolbar();
                // if (IsUsageHistoryVisible) {
                //     SirenixEditorGUI.BeginLegendBox();
                //     DrawUsageHistory();
                //     SirenixEditorGUI.EndLegendBox();
                // }
                GUILayout.Space(5);
            }
            base.OnImGUI();
            return;

            // void DrawUsageHistory() {
            //     SirenixEditorGUI.BeginVerticalList(true, true, GUILayout.MaxHeight(200));
            //     int lastXIndex = math.max(_richLabelConfig.PersistentRichLabelUsageData.Count - 10, 0);
            //     for (int i = _richLabelConfig.PersistentRichLabelUsageData.Count - 1; i >= lastXIndex; i--) {
            //         var persistentRichLabelUsageData = _richLabelConfig.PersistentRichLabelUsageData[i];
            //         var guidsByCategories = RichLabelEditorUtilities.GetGuidsByCategories(persistentRichLabelUsageData.RichLabelGuids, _richLabelConfig);
            //         var rect = SirenixEditorGUI.BeginListItem(false, RichLabelStyles.ListItem, GUILayout.Height(20));
            //         SirenixEditorGUI.BeginHorizontalPropertyLayout(null);
            //         
            //         if(GUI.Button(rect, GUIContent.none, RichLabelStyles.ListItem)) {
            //             _chosenEntries.Clear();
            //             _chosenEntries.AddRange(persistentRichLabelUsageData.RichLabelGuids.Select(p => new RichLabelUsageEntry(p, true)));
            //             _onChange?.Invoke(_chosenEntries); 
            //         }
            //         
            //         foreach (var guid in guidsByCategories.Values.SelectMany(p => p).Distinct()) {
            //             if (_richLabelConfig.TryGetLabel(guid, out var label)) {
            //                 float elementWidth = (position.width - 20) / _richLabelConfig.CategoriesCount;
            //                 GUILayout.Label(label.Name, RichLabelStyles.HistoryElementLabelStyle, GUILayout.Width(elementWidth));
            //             }
            //         }
            //
            //         GUILayout.FlexibleSpace();
            //         SirenixEditorGUI.EndHorizontalPropertyLayout();
            //         SirenixEditorGUI.EndListItem();
            //     }
            //
            //     SirenixEditorGUI.EndVerticalList();
            // }
        }

        protected override void DrawTips() {
            base.DrawTips();
            var rect = GUILayoutUtility.GetRect(15, 75, 15, 15);
            Rect iconRect = rect;
            iconRect.width = 15;

            Rect labelRect = rect;
            labelRect.x += 15;
            labelRect.width -= 15;

            GUI.DrawTexture(iconRect, _rightClickIcon);
            GUI.Label(labelRect, "Exclude");
        }

        protected override bool CanDrawLabel(RichLabel label) {
            bool isSearchStringEmpty = string.IsNullOrWhiteSpace(_searchString);

            if (!(isSearchStringEmpty || label.Name.ToLower().Contains(_searchString.ToLower()))) {
                return false;
            }

            RichLabelUsageEntry chosenEntry = _chosenEntries.FirstOrDefault(p => p.RichLabelGuid == label.Guid);
            if (chosenEntry != null) {
                return true;
            }

            var sharedUsageData = _richLabelConfig.PersistentRichLabelUsageData.Where(p => p.RichLabelGuids.Contains(label.Guid));
            foreach (var entry in _chosenEntries) {
                if (!entry.Include) {
                    sharedUsageData = sharedUsageData.Where(p => !p.RichLabelGuids.Contains(entry.RichLabelGuid));
                }
            }

            foreach (var entry in _chosenEntries) {
                if (entry.Include) {
                    sharedUsageData = sharedUsageData.Where(p => p.RichLabelGuids.Contains(entry.RichLabelGuid));
                }
            }

            return sharedUsageData.Any(w => w.RichLabelGuids.Contains(label.Guid));
        }

        protected override void DrawRichLabel(RichLabel label, RichLabelCategory category) {
            var chosenEntry = _chosenEntries.FirstOrDefault(p => p.RichLabelGuid == label.Guid);
            if (chosenEntry != null) {
                var guiStyle = chosenEntry.Include ? RichLabelStyles.IncludedLabel : RichLabelStyles.ExcludedLabel;
                var buttonState = DrawDualClickButton(LabelRect, new GUIContent(label.Name), guiStyle);
                switch (buttonState) {
                    case ButtonState.LeftClicked when chosenEntry.Include:
                        _chosenEntries.Remove(chosenEntry);
                        _onChange?.Invoke(_chosenEntries);
                        break;
                    case ButtonState.RightClicked when !chosenEntry.Include:
                        _chosenEntries.Remove(chosenEntry);
                        _onChange?.Invoke(_chosenEntries);
                        break;
                    case ButtonState.LeftClicked when !chosenEntry.Include:
                    case ButtonState.RightClicked when chosenEntry.Include:
                        chosenEntry.Include = !chosenEntry.Include;
                        _onChange?.Invoke(_chosenEntries);
                        break;
                    case ButtonState.None:
                    default:
                        break;
                }
            } else {
                var guiStyle = RichLabelStyles.NeutralLabel;
                var buttonState = DrawDualClickButton(LabelRect, new GUIContent(label.Name), guiStyle);
                switch (buttonState) {
                    case ButtonState.LeftClicked:
                        _chosenEntries.Add(new RichLabelUsageEntry(label.Guid, true));
                        _onChange?.Invoke(_chosenEntries);
                        break;
                    case ButtonState.RightClicked:
                        _chosenEntries.Add(new RichLabelUsageEntry(label.Guid, false));
                        _onChange?.Invoke(_chosenEntries);
                        break;
                    case ButtonState.None:
                    default:
                        break;
                }
            }
        }

        static ButtonState DrawDualClickButton(Rect position, GUIContent content, GUIStyle style) {
            int controlId = GUIUtility.GetControlID("Button".GetHashCode(), FocusType.Passive, position);
            Event current = Event.current;

            switch (current.type) {
                case EventType.MouseDown:
                    if (HitTest(position, current.mousePosition)) {
                        GrabMouseControl(controlId);
                        current.Use();
                    }

                    break;

                case EventType.MouseUp when current.button == 0:
                    if (HasMouseControl(controlId)) {
                        ReleaseMouseControl();
                        current.Use();
                        if (HitTest(position, current.mousePosition)) {
                            GUI.changed = true;
                            return ButtonState.LeftClicked;
                        }
                    }

                    break;
                case EventType.MouseUp:
                    if (HasMouseControl(controlId)) {
                        ReleaseMouseControl();
                        current.Use();
                        if (HitTest(position, current.mousePosition)) {
                            GUI.changed = true;
                            return ButtonState.RightClicked;
                        }
                    }

                    break;
                case EventType.MouseDrag:
                    if (HasMouseControl(controlId)) {
                        current.Use();
                    }

                    break;
                case EventType.Repaint:
                    style.Draw(position, content, controlId, false, position.Contains(Event.current.mousePosition));
                    break;
            }

            return ButtonState.None;

            void GrabMouseControl(int id) {
                var methodInfo = typeof(GUI).GetMethod("GrabMouseControl", BindingFlags.NonPublic | BindingFlags.Static);
                if (methodInfo != null) methodInfo.Invoke(null, new object[] { id });
            }

            bool HasMouseControl(int id) {
                var type = typeof(GUI);
                var methodInfo = type.GetMethod("HasMouseControl", BindingFlags.NonPublic | BindingFlags.Static);
                if (methodInfo != null) {
                    object value = methodInfo.Invoke(null, new object[] { id });
                    return Convert.ToBoolean(value);
                }

                return false;
            }

            void ReleaseMouseControl() {
                var methodInfo = typeof(GUI).GetMethod("ReleaseMouseControl", BindingFlags.NonPublic | BindingFlags.Static);
                if (methodInfo != null) methodInfo.Invoke(null, null);
            }

            bool HitTest(Rect rect, Vector2 point) {
                return point.x >= rect.xMin && point.x < rect.xMax &&
                       point.y >= rect.yMin && point.y < rect.yMax;
            }
        }

        enum ButtonState {
            None,
            LeftClicked,
            RightClicked
        }
    }
}