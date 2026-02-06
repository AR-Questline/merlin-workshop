using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Main.Scenes.SubdividedScenes;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Editor.ToolbarTools.TopToolbars {
    public class TopToolbarDropdown : ITopToolbarElement {
        readonly DropdownEntree[] _dropdownEntrees;
        readonly int _defaultWidth;
        readonly GUIContent _guiContent;
        GenericMenu _genericMenu;
        readonly Func<bool> _additionalEnabledRequirement;
        readonly bool _shouldRegenerateMenuAlways;
        int _width;
        GUILayoutOption _widthOption;

        public string Name { get; }
        public bool DefaultEnabled { get; }
        public bool CanChangeSide => true;
        public TopToolbarButtons.Side DefaultSide { get; }

        public IEnumerable<string> CustomKeys { get; }

        string WidthKey => $"{((ITopToolbarElement)this).MainKey}.Width";

        bool ITopToolbarElement.Enabled {
            get => (_additionalEnabledRequirement?.Invoke() ?? true) && EditorPrefs.GetBool($"{((ITopToolbarElement)this).MainKey}.Enabled", DefaultEnabled);
            set => EditorPrefs.SetBool($"{((ITopToolbarElement)this).MainKey}.Enabled", value);
        }

        public TopToolbarDropdown(string name, string tooltip, in DropdownEntree[] dropdownEntrees, int width,
                                  TopToolbarButtons.Side defaultSide, bool defaultEnabled, Func<bool> additionalEnabledRequirement = null, bool shouldRegenerateMenuAlways = false) {
            Name = name;
            CustomKeys = new[] {
                WidthKey,
            };
            DefaultEnabled = defaultEnabled;
            DefaultSide = defaultSide;
            _guiContent = new GUIContent(((ITopToolbarElement)this).ShowName, tooltip);
            _dropdownEntrees = dropdownEntrees;
            _defaultWidth = width;
            _additionalEnabledRequirement = additionalEnabledRequirement;
            _shouldRegenerateMenuAlways = shouldRegenerateMenuAlways;

            GenerateMenu();
            AfterResetPrefsBasedValues();
        }

        void GenerateMenu() {
            _genericMenu = new GenericMenu();
            foreach (DropdownEntree entree in _dropdownEntrees) {
                if (entree.isSeparator) {
                    _genericMenu.AddSeparator("");
                    continue;
                }
                if (entree.isDisabled?.Invoke() is null or false) {
                    _genericMenu.AddItem(new GUIContent(entree.name, entree.tooltip), false, new GenericMenu.MenuFunction(entree.action));
                }
            }
        }

        public void SettingsGUI() {
            EditorGUILayout.LabelField("Width", GUILayout.Width(ITopToolbarElement.DefaultLabelWidth));
            EditorGUI.BeginChangeCheck();
            _width = EditorGUILayout.IntSlider("", _width, 0, 150);
            if (EditorGUI.EndChangeCheck()) {
                EditorPrefs.SetInt(WidthKey, _width);
                _widthOption = GUILayout.Width(_width);
            }
        }

        public void AfterResetPrefsBasedValues() {
            _width = EditorPrefs.GetInt(WidthKey, _defaultWidth);
            _widthOption = GUILayout.Width(_width);
        }

        public void OnGUI() {
            _guiContent.text = ((ITopToolbarElement)this).ShowName; // Apply override name
            if (GUILayout.Button(_guiContent, EditorStyles.toolbarButton, _widthOption)) {
                if (_shouldRegenerateMenuAlways) {
                    GenerateMenu();
                }
                _genericMenu.DropDown(new Rect(Event.current.mousePosition, new Vector2(0, 0)));
            }
        }
    }
    public readonly struct DropdownEntree {
        public readonly string name;
        public readonly Action action;
        public readonly string tooltip;
        public readonly bool isSeparator;
        public readonly Func<bool> isDisabled;

        DropdownEntree(bool isSeparator) {
            name = null;
            action = null;
            tooltip = null;
            this.isSeparator = isSeparator;
            isDisabled = null;
        }
        public DropdownEntree(string name, Action action, string tooltip = null, Func<bool> isDisabled = null) {
            this.name = name;
            this.action = action;
            this.tooltip = tooltip;
            this.isSeparator = false;
            this.isDisabled = isDisabled;
        }
        
        public static DropdownEntree Separator() {
            return new DropdownEntree(true);
        }
    }
}