using System;
using System.Collections.Generic;
using System.IO;
using Awaken.TG.Editor.BalanceTool.Data;
using Awaken.TG.Main.UIToolkit;
using Awaken.Utility.Editor.UTK;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Editor.BalanceTool.Presets {
    public class BalanceToolStatsPresetSaveWindow : EditorWindowPresenter<BalanceToolStatsPresetSaveWindow> {
        const string PresetPath = "Assets/Resources/Data/BalanceTool/StatsPresets";

        Button _confirmButton;
        Button _cancelButton;
        TextField _presetNameField;
        Label _presetPathLabel;
        Label _infoLabel;
        Label _validationErrorLabel;

        static BalanceToolData s_toSave;
        string _infoTextFormat = "Saves the following statistics fields [<b>{0}</b>] as a preset asset.";
        
        public BalanceToolStatsPresetSaveWindow() {
            WindowName = "Preset Save Window";
        }
        
        public static void ShowWindow(BalanceToolData data) {
            s_toSave = data;
            BalanceToolStatsPresetSaveWindow wnd = GetWindow();
            wnd.minSize = new Vector2(450, 180);
            wnd.maxSize = new Vector2(600, 200);
            wnd.ShowModal();
        }
        
        public override void CreateGUI() {
            // always call base.CreateGUI() first to properly setup the window
            base.CreateGUI();
            SetupInfo();
            SetupButtons();
            SetupInput();
        }

        protected override void CacheVisualElements(VisualElement windowRoot) {
            _confirmButton = windowRoot.Q<Button>("confirm");
            _cancelButton = windowRoot.Q<Button>("cancel");
            _presetNameField = windowRoot.Q<TextField>("file-name");
            _presetPathLabel = windowRoot.Q<Label>("file-path");
            _infoLabel = windowRoot.Q<Label>("save-info");
            _validationErrorLabel = windowRoot.Q<Label>("validation-info");
        }
        
        void SetupInfo() {
            List<string> statNames = new () {
                nameof(StatEntry.BaseValue),
                nameof(StatEntry.AddPerLevel),
                nameof(StatEntry.description)
            };
            
            string abc = string.Join(", ", statNames);
            _infoLabel.text = string.Format(_infoTextFormat, abc);
            _validationErrorLabel.SetActiveOptimized(false);
        }
        
        void SetupButtons() {
            _confirmButton.clicked += SavePreset;
            _cancelButton.clicked += Close;
        }
        
        void SetupInput() {
            _presetNameField.value = DateTime.Now.ToString("yyyy-MM-dd HHmmss");;
            _presetPathLabel.text = $"Preset path: <i><b>{PresetPath}</b></i>";
        }

        void SavePreset() {
            _validationErrorLabel.SetActiveOptimized(false);

            if (string.IsNullOrEmpty(_presetNameField.value)) {
                _validationErrorLabel.text = "Please enter a name for the preset.";
                _validationErrorLabel.SetActiveOptimized(true);
                return;
            }
            
            BalanceToolStatsPreset preset = CreateInstance<BalanceToolStatsPreset>();
            string filePath = Path.Combine(PresetPath, _presetNameField.value + ".asset");
            
            if (File.Exists(filePath)) {
                _validationErrorLabel.text = "There is already a file with the same name in this path";
                _validationErrorLabel.SetActiveOptimized(true);
                return;
            }
            
            if(!Directory.Exists(PresetPath)) {
                Directory.CreateDirectory(PresetPath);
            }
            
            preset.CreatePreset(s_toSave);
            AssetDatabase.CreateAsset(preset, filePath);
            Close();
        }
    }
}
