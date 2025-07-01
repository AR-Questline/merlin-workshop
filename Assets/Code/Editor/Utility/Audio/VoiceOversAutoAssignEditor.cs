using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Awaken.TG.Editor.Utility.StoryGraphs.Converter;
using Awaken.TG.Main.Stories.Actors;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.Audio {
    public class VoiceOversAutoAssignEditor : OdinEditorWindow {
        List<VoiceOverData> _voiceOvers;

        [TitleGroup("Sections")]
        [HorizontalGroup("Sections/Split", Width = 400), BoxGroup("Sections/Split/PathToAudio"), HideLabel]
        [OnValueChanged(nameof(ForceRefresh)), ShowInInspector, FolderPath]
        string _pathToAudio;
        
        List<string> _audioData = new ();
        
        [HorizontalGroup("Sections/Split", Width = 400), BoxGroup("Sections/Split/PathToCSV"), HideLabel]
        [ShowInInspector, Sirenix.OdinInspector.FilePath(Extensions = "csv"), OnValueChanged(nameof(ForceRefresh))]
        string _pathToCSV;
        
        List<string> _csvData = new ();
        
        [HorizontalGroup("Sections/Split"), BoxGroup("Sections/Split/Actor Filter"), PropertySpace(-20), HideLabel]
        // [PropertyTooltip("Apply the Actor Filter in scenarios where identical dialogue lines may be attributed to multiple actors.")]
        [ShowInInspector, OnValueChanged(nameof(ForceRefresh))]
        ActorRef _actorRef;

        List<VoiceOverData> _filteredVoiceOvers = new ();

        [TitleGroup("Sections")]
        [ShowInInspector, SerializeField, TableList(HideToolbar = true, AlwaysExpanded = true, DefaultMinColumnWidth = 100, IsReadOnly = true)]
        List<VoiceOverDataExtended> dataGrouped = new ();
        
        // === Initialization
        [MenuItem("TG/Audio/VoiceOvers Auto Assign Editor")]
        static void CreateWindow() {
            var window = GetWindow<VoiceOversAutoAssignEditor>();
            window.titleContent = new GUIContent("VoiceOvers Auto Assign Editor");
            // window.position = GUIHelper.GetEditorWindowRect().AlignCenter(1920, 1080);
            window.Init();
        }

        public void Init() {
            ClearAll();
            PopulateVoiceOversData();
        }

        protected override void OnImGUI() {
            base.OnImGUI();
            
            if (_voiceOvers == null) {
                PopulateVoiceOversData();
                ForceRefresh();
            }
        }
        
        void PopulateVoiceOversData() {
            VoiceOverData.ResetCache();
            _voiceOvers = new List<VoiceOverData>();
            foreach (var audioFilePath in EditorAudioUtils.GetAllVoiceOverPaths()) {
                _voiceOvers.Add(new VoiceOverData(audioFilePath));
            }
        }

        void RefreshActorFilter() {
            string actorName = ActorsRegister.Get.Editor_GetActorName(_actorRef.guid);
            Regex regexActorName = new($"({actorName})", RegexOptions.IgnoreCase);
            IEnumerable<VoiceOverData> filtered = _voiceOvers;
            if (_csvData != null) {
                filtered = filtered.Where(v => _csvData.Contains(v.dialogueLine))
                    .OrderBy(v => _csvData.IndexOf(v.dialogueLine));
            }

            if (!(string.IsNullOrEmpty(_actorRef.guid) || _actorRef == DefinedActor.None.ActorRef)) {
                filtered = filtered.Where(v => regexActorName.IsMatch(v.actor));
            }

            filtered = filtered.GroupBy(x => new { x.actor, x.dialogueLine }).Select(d => d.First());
            _filteredVoiceOvers = filtered.ToList();
        }

        void RefreshCSV() {
            if (string.IsNullOrEmpty(_pathToCSV)) {
                return;
            }

            using var reader = new StreamReader(_pathToCSV);
            // using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            // _csvData = new List<string>();
            // while (csv.Read()) {
            //     string s = csv.GetField<string>(0);
            //     if (!string.IsNullOrEmpty(s)) {
            //         _csvData.Add(s);
            //     }
            // }
        }

        void RefreshAudio() {
            if (string.IsNullOrEmpty(_pathToAudio)) {
                return;
            }

            _audioData = new List<string>();
            foreach (var audioFilePath in EditorAudioUtils.GetAllAudioFilePathsFromDirectory(_pathToAudio)) {
                _audioData.Add(audioFilePath);
            }
        }

        void RefreshGroupedData() {
            dataGrouped.Clear();
            
            int elements = Math.Max(_csvData.Count, _audioData.Count);
            for (int i = 0; i < elements; i++) {
                dataGrouped.Add(new VoiceOverDataExtended {
                    csvData = _csvData.Count > i ? _csvData[i] : string.Empty,
                    audioData = _audioData.Count > i ? _audioData[i] : string.Empty,
                    voiceOverData = _filteredVoiceOvers?.Count > i ? _filteredVoiceOvers[i] : null
                });
            }
        }

        [TitleGroup("Sections/Buttons"), HorizontalGroup("Sections/Buttons/Left", Width = 200),
         Button(ButtonSizes.Large)]
        void ForceRefresh() {
            RefreshCSV();
            RefreshAudio();
            RefreshActorFilter();
            RefreshGroupedData();
        }

        [HorizontalGroup("Sections/Buttons/Left", Width = 200), Button(ButtonSizes.Large)]
        void ClearAll() {
            _actorRef = DefinedActor.None.ActorRef;
            _pathToAudio = string.Empty;
            _pathToCSV = string.Empty;
            _csvData.Clear();
            _audioData.Clear();
            _filteredVoiceOvers.Clear();
            dataGrouped.Clear();
        }

        [HorizontalGroup("Sections/Buttons/Left", Width = 200), Button(ButtonSizes.Large)]
        void AssignVoiceOvers() {
            if (_csvData.Count != _audioData.Count) {
                string errMsg = $"The number of dialogue lines in the CSV file ({_csvData.Count}) does not match the number of audio files in the folder ({_audioData.Count}). Please check your data.";
                EditorUtility.DisplayDialog("Error", errMsg, "OK");
                return;
            }

            if (_csvData.Count != _filteredVoiceOvers.Count) {
                string errMsg = $"The number of dialogue lines in the CSV file ({_csvData.Count}) does not match the number of filtered voice overs ({_filteredVoiceOvers.Count}). Please check your data.";
                EditorUtility.DisplayDialog("Error", errMsg, "OK");
                return;
            }

            if (!CheckCSVMatchFiltered()) {
                string errMsg = "The CSV file does not match the filtered voice overs. Please check your data.";
                EditorUtility.DisplayDialog("Error", errMsg, "OK");
                return;
            }
            
            int maxElementsAbleToAssign = Math.Min(dataGrouped.Count, _audioData.Count);
            for (int i = 0; i < dataGrouped.Count; i++) {
                string curDialogueLine = dataGrouped[i].DialogueLine;
                if (EditorUtility.DisplayCancelableProgressBar("Assigning Voice Overs",
                        $"{i + 1} / {maxElementsAbleToAssign}: {curDialogueLine}", 
                        (float)i / dataGrouped.Count)) {
                    break;
                }
                
                var newAudioPath = dataGrouped[i].audioData;

                if (!dataGrouped[i].IsValidToAudioReplace) {
                    Log.Important?.Warning(
                        $"Voice over: {dataGrouped[i].DialogueLine} by actor {dataGrouped[i].Actor} is not valid for audio update. Skipping. (Check if the dialogue line is the same in the CSV file)");
                    continue;
                }
                
                // --- Assign new audio path to all voice overs with the same dialogue line and actor
                var dataGroupedEntry = dataGrouped[i];
                var voiceOverDuplicates = _voiceOvers.Where(x => x.dialogueLine == dataGroupedEntry.DialogueLine && x.actor == dataGroupedEntry.Actor);
                foreach (var duplicatedVO in voiceOverDuplicates) {
                    duplicatedVO.AssignNewAudioReplacement(newAudioPath);
                }
            }

            EditorUtility.ClearProgressBar();
        }

        [HorizontalGroup("Sections/Buttons/Left", Width = 500), Button(ButtonSizes.Large)]
        void AssignVoiceOversAndUpdateAllStoryGraphsVoiceOversReferences() {
            AssignVoiceOvers();
            GraphConverterUtils.UpdateAllStoryGraphsVoiceOversReferences();
        }

        bool CheckCSVMatchFiltered() {
            if(_csvData.Count != _filteredVoiceOvers.Count) {
                return false;
            }
            
            for (int i = 0; i < _csvData.Count; i++) {
                if (_csvData[i] != _filteredVoiceOvers[i].dialogueLine) {
                    return false;
                }
            }
            return true;
        }
    }
}