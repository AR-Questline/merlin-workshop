using System.Linq;
using Awaken.TG.Editor.Utility.RichLabels.Configs;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.RichLabels {
    public abstract class RichLabelBrowser : OdinEditorWindow {
        protected const int ColumnWidth = 120;
        protected const int ColumnItemHeight = 30;
        
        protected RichLabelConfig _richLabelConfig;
        protected string _searchString;

        const string PathToLeftClickIcon = "Assets/2DAssets/UI/Generic/mouse_left.png";
        
        static readonly string PersistentKeyShowTips = $"{nameof(RichLabelBrowser)};{nameof(ShowTips)}";
        static Texture s_leftClickIcon;
        
        RichLabelCategory[] _categories;
        Vector2 _scrollPosition;

        bool ShowTips {
            get => EditorPrefs.GetBool(PersistentKeyShowTips, false);
            set => EditorPrefs.SetBool(PersistentKeyShowTips, value);
        }
        
        public virtual void ShowWindow(Rect source, RichLabelConfig richLabelConfig) {
            _richLabelConfig = richLabelConfig;

            Rect windowRect = source;
            bool isOnTheLeftOfTheScreen = IsOnTheLeftOfTheScreen(windowRect);

            if (!s_leftClickIcon) {
                s_leftClickIcon = (Texture)AssetDatabase.LoadAssetAtPath(PathToLeftClickIcon, typeof(Texture));
            }

            int windowWidth = ColumnWidth * _richLabelConfig.CategoriesCount + 75;
            Vector2 windowSize = new(windowWidth, 500);
            Vector2 windowPosition = GUIUtility.GUIToScreenPoint(windowRect.position);
            windowPosition.x += isOnTheLeftOfTheScreen ? source.width : -windowSize.x;
            windowPosition.y -= source.height;
            windowRect.position = windowPosition;
            ShowAsDropDown(windowRect, windowSize);
        }

        protected override void OnImGUI() {
            _categories ??= _richLabelConfig.GetPossibleCategories().ToArray();

            // SirenixEditorGUI.BeginHorizontalToolbar();
            GUILayout.Label("Rich Label Browser");
            GUILayout.FlexibleSpace();
            var iconContent = EditorGUIUtility.IconContent(ShowTips ? "animationvisibilitytoggleon@2x" : "animationvisibilitytoggleoff@2x");
            iconContent.text = "Tips";
            iconContent.tooltip = "Show or Hide tips";
            // ShowTips = SirenixEditorGUI.ToolbarToggle(ShowTips, iconContent);
            // SirenixEditorGUI.EndHorizontalToolbar();
            
            // SirenixEditorGUI.BeginLegendBox();
            
            if (ShowTips) {
                GUILayout.Space(10f);
                // SirenixEditorGUI.BeginHorizontalPropertyLayout(null);
                DrawTips();
                GUILayout.FlexibleSpace();
                // SirenixEditorGUI.EndHorizontalPropertyLayout();
                GUILayout.Space(10f);
            }
            
            // SirenixEditorGUI.BeginIndentedHorizontal();
            var rect = GUILayoutUtility.GetRect(100, 200, 20, 30);
            // _searchString = SirenixEditorGUI.SearchField(rect, _searchString);
            // SirenixEditorGUI.EndIndentedHorizontal();

            // SirenixEditorGUI.BeginBox();
            // _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            // GUILayout.BeginHorizontal();
            // foreach (RichLabelCategory currentCategory in _categories) {
            //     SirenixEditorGUI.BeginBox(GUILayout.Width(ColumnWidth));
            //     SirenixEditorGUI.BeginBoxHeader();
            //     GUILayout.Label(currentCategory.Name);
            //     SirenixEditorGUI.EndBoxHeader();
            //     foreach (RichLabel label in currentCategory.Labels.Where(CanDrawLabel)) {
            //         DrawRichLabel(label, currentCategory);
            //     }
            //
            //     SirenixEditorGUI.EndBox();
            // }
            // GUILayout.EndHorizontal();
            // GUILayout.EndScrollView();
            // SirenixEditorGUI.EndBox();
            //
            // SirenixEditorGUI.EndLegendBox();
        }

        protected virtual void DrawTips() {
            var rect = GUILayoutUtility.GetRect(15, 75, 15, 15);
            Rect iconRect = rect;
            iconRect.width = 15;

            Rect labelRect = rect;
            labelRect.x += 15;
            labelRect.width -= 15;

            GUI.DrawTexture(iconRect, s_leftClickIcon);
            GUI.Label(labelRect, "Include");
        }

        protected virtual bool CanDrawLabel(RichLabel label) {
            return string.IsNullOrWhiteSpace(_searchString) || label.Name.ToLower().Contains(_searchString.ToLower());
        }

        protected virtual void DrawRichLabel(RichLabel label, RichLabelCategory category) {
            Rect labelRect = GUILayoutUtility.GetRect(ColumnWidth, ColumnWidth, ColumnItemHeight, ColumnItemHeight);
            var guiStyle = RichLabelStyles.NeutralLabel;
            // SirenixEditorGUI.SDFIconButton(labelRect, label.Name, null, IconAlignment.RightOfText, guiStyle);
        }

        static bool IsOnTheLeftOfTheScreen(Rect windowRect) {
            int currentResWidth = Screen.currentResolution.width;
            Vector2 screenPoint = GUIUtility.GUIToScreenPoint(windowRect.position);

#if UNITY_EDITOR_WIN
            System.Drawing.Point cursorPoint = new();
            if (GetCursorPos(ref cursorPoint)) {
                screenPoint = new Vector2(cursorPoint.X, cursorPoint.Y);
            }
#endif
            if (screenPoint.x >= 0) {
                if (screenPoint.x > currentResWidth) {
                    screenPoint.x -= currentResWidth;
                }

                return screenPoint.x < currentResWidth / 2f;
            }

            return Mathf.Abs(screenPoint.x) > currentResWidth / 2f;

#if UNITY_EDITOR_WIN
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            static extern bool GetCursorPos(ref System.Drawing.Point lpPoint);
#endif
        }
    }
}