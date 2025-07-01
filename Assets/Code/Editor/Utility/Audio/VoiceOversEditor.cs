using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using SearchField = UnityEditor.IMGUI.Controls.SearchField;

namespace Awaken.TG.Editor.Utility.Audio {
    public class VoiceOversEditor : OdinEditorWindow {
        static readonly string[] StoryFolders = {
            "Assets/Data/Templates/Stories/",
            "Assets/Data/Templates/Stories_Clean/",
        };
        
        public static string lastSearchedDirectory;

        
        Vector2 _scrollPosition;
        List<VoiceOverData> _voiceOvers;
        SearchField _dialogueSearch, _actorSearch, _audioReplacementSearch;
        string _dialogueFilter, _actorFilter, _audioReplacementFilter;
        bool _filterChanged, _isFiltering, _wasFiltering;

        FilterMode _filterMode = FilterMode.ShowAll;
        
        bool _onlyUsedInStory;

        [ShowInInspector, TableList(ShowPaging = true, NumberOfItemsPerPage = 10, AlwaysExpanded = true, IsReadOnly = true)] 
        List<VoiceOverData> _filteredVoiceOvers;
        List<VoiceOverData> _tmp;

        enum FilterMode {
            [Tooltip("Displays all dialogue lines")] ShowAll,
            [Tooltip("Displays dialogue lines that don't have VO assigned or VO is mismatched (see Mismatch checkbox tooltip)")] ShowMissing,
            [Tooltip("Displays dialogue lines that have VO assigned, but VO is mismatched (see Mismatch checkbox tooltip)")] ShowMismatches,
        }
        
        // === Initialization
        [MenuItem("TG/Audio/Voice Overs Editor")]
        static void Init() {
            var window = GetWindow<VoiceOversEditor>();
            window.titleContent = new GUIContent("VoiceOvers Editor");
            // window.position = GUIHelper.GetEditorWindowRect().AlignCenter(1200, 600);
            window.PopulateData();
        }

        void PopulateData() {
            VoiceOverData.ResetCache();
            _voiceOvers = new List<VoiceOverData>();
            // group by story graph name
            foreach (var audioFilePath in EditorAudioUtils.GetAllVoiceOverPaths()) {
                _voiceOvers.Add(new VoiceOverData(audioFilePath));
            }

            _filteredVoiceOvers = _voiceOvers;
            _dialogueSearch = new SearchField();
            _dialogueFilter = string.Empty;
            _actorSearch = new SearchField();
            _actorFilter = string.Empty;
            _audioReplacementSearch = new SearchField();
            _audioReplacementFilter = string.Empty;
            lastSearchedDirectory = Application.dataPath;
        }

        // === Drawing
        protected override void OnImGUI() {
            if (_voiceOvers == null) {
                PopulateData();
            }

            // --- draw search bars
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Dialogue Line:", GUILayout.Width(90));
            var rect = GUILayoutUtility.GetRect(100, 150, 18, 18, GUILayout.ExpandWidth(false));
            _dialogueFilter = _dialogueSearch.OnGUI(rect, _dialogueFilter);

            GUILayout.Space(15);
            
            GUILayout.Label("Actor:", GUILayout.Width(40));
            rect = GUILayoutUtility.GetRect(100, 150, 18, 18, GUILayout.ExpandWidth(false));
            _actorFilter = _actorSearch.OnGUI(rect, _actorFilter);
            
            GUILayout.Space(15);
            
            GUILayout.Label("Audio Replacement:", GUILayout.Width(120));
            rect = GUILayoutUtility.GetRect(100, 150, 18, 18, GUILayout.ExpandWidth(false));
            _audioReplacementFilter = _audioReplacementSearch.OnGUI(rect, _audioReplacementFilter);
            
            GUILayout.Space(15);
            _filterMode = (FilterMode)EditorGUILayout.EnumPopup(_filterMode);
            
            GUILayout.Space(15);
            GUILayout.Label(new GUIContent("Only Used:", "Enable this to see only dialogue lines that are used in Story (connected to story flow)"), GUILayout.Width(80));
            _onlyUsedInStory = EditorGUILayout.Toggle(_onlyUsedInStory);
            
            if (EditorGUI.EndChangeCheck()) {
                RefreshFilter();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("Resolve all mismatches"))) {
                if (EditorUtility.DisplayDialog("Resolve all mismatches", "Are you sure you want to resolve all mismatches?", "Yes", "No")) {
                    foreach (var data in _voiceOvers!.Where(v => v.TranslationMismatch)) {
                        data.ResolveMismatch();
                    }
                }
            }

            if (GUILayout.Button(new GUIContent("Remove Missing Entries"))) {
                List<string> missingEntriesPaths = _voiceOvers!.Where(v => v.MissingEntry).Select(v => v.AudioFilePath).ToList();
                FMODAudioToEventsExporter.RemoveEvents(missingEntriesPaths);
                foreach (var path in missingEntriesPaths) {
                    File.Delete(path);
                }
                PopulateData();
                RefreshFilter();
                EditorGUILayout.EndHorizontal();
                return;
            }
            
            if (GUILayout.Button(new GUIContent("Refresh Data"))) {
                PopulateData();
                RefreshFilter();
                EditorGUILayout.EndHorizontal();
                return;
            }
            
            EditorGUILayout.EndHorizontal();
            
            try {
                base.OnImGUI();
                if (_wasFiltering) {
                    // --- apply filtering in next frame to avoid property drawer errors
                    _filteredVoiceOvers = _tmp;
                    _wasFiltering = false;
                }
            } catch (Exception) {
                // Suspend drawing errors
            }

            if (_filterChanged && !_isFiltering) {
                FilterData();
                _filterChanged = false;
                _wasFiltering = true;
            }
        }

        void RefreshFilter() {
            _filterChanged = true;
            FilterData();
        }

        async void FilterData() {
            _isFiltering = true;
            await Task.Run(() => {
                IEnumerable<VoiceOverData> filtered = _voiceOvers;
                
                if (_onlyUsedInStory) {
                    filtered = filtered.Where(v => v.UsedInStory);
                }

                if (_filterMode == FilterMode.ShowMissing) {
                    filtered = filtered.Where(v => v is { MissingEntry: false, VOMissingOrInvalid: true });
                } else if (_filterMode == FilterMode.ShowMismatches) {
                    filtered = filtered.Where(v => v is { MissingEntry: false, TranslationMismatch: true });
                }

                bool filterByStoryFolders = _onlyUsedInStory || _filterMode != FilterMode.ShowAll;
                if (filterByStoryFolders) {
                    filtered = filtered.Where(v => StoryFolders.Any(f => v.StoryGraphPath.StartsWith(f)));
                }
                
                if (!string.IsNullOrWhiteSpace(_dialogueFilter)) {
                    filtered = filtered.Where(v =>
                        v?.dialogueLine?.Contains(_dialogueFilter, StringComparison.InvariantCultureIgnoreCase) ?? false);
                }

                if (!string.IsNullOrWhiteSpace(_actorFilter)) {
                    filtered = filtered.Where(v =>
                        v?.actor?.Contains(_actorFilter, StringComparison.InvariantCultureIgnoreCase) ?? false);
                }

                if (!string.IsNullOrWhiteSpace(_audioReplacementFilter)) {
                    filtered = filtered.Where(v =>
                        v?.AudioReplacementName?.Contains(_audioReplacementFilter, StringComparison.InvariantCultureIgnoreCase) ?? false);
                }
                
                _tmp = filtered.Take(100).ToList();
            });
            _isFiltering = false;
        }
    }
}
