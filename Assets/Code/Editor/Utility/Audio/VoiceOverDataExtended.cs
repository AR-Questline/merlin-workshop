using System;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.Audio {
    [Serializable]
    public class VoiceOverDataExtended {
        [HideInTables] public VoiceOverData voiceOverData;
        
        [TableColumnWidth(400), GUIColor(nameof(LabelColor))]
        public string audioData;
        [TableColumnWidth(400), GUIColor(nameof(LabelColor))]
        public string csvData;
        
        [ShowInInspector, TableColumnWidth(450)]
        public string DialogueLine => voiceOverData?.dialogueLine;
        
        [ShowInInspector, TableColumnWidth(90)]
        public string Actor => voiceOverData?.actor;

        [ShowInInspector, LabelText(""), TableColumnWidth(150)]
        public string AssignedAudio => voiceOverData?.DisplayedAudioName;

        public bool IsValidToAudioReplace => voiceOverData != null
                                             && !string.IsNullOrEmpty(audioData)
                                             && !string.IsNullOrEmpty(csvData)
                                             && DialogueLine == csvData;
        
        Color LabelColor => IsValidToAudioReplace ? Color.white : Color.red;
        Color PlayStopColor => voiceOverData?.PlayStopColor ?? Color.gray;
        bool IsPlaying => voiceOverData is { IsPlaying: true };
        
        [Button(SdfIconType.Play), GUIColor(nameof(PlayStopColor)), HideLabel, HorizontalGroup("Buttons", Width = 25), HideIf(nameof(IsPlaying))]
        public void PlayPreview() {
            voiceOverData?.PlayPreview();
        }

        [Button(SdfIconType.Stop), GUIColor(nameof(PlayStopColor)), HideLabel, HorizontalGroup("Buttons", Width = 25), ShowIf(nameof(IsPlaying))]
        public void StopPlayingPreview() {
            voiceOverData?.StopPlayingPreview();
        }
        
        [Button("Copy Id", ButtonSizes.Small), HorizontalGroup("Buttons", Width = 60)]
        void CopyIdToClipBoard() {
            voiceOverData?.CopyIdToClipBoard();
        }

        [Button("Clear", ButtonSizes.Small), HorizontalGroup("Buttons", Width = 60)]
        void Clear() {
            voiceOverData?.ClearAudioReplacementMetaData();
        }

        [Button("Graph \u21b5", ButtonSizes.Small), HorizontalGroup("Buttons", Width = 60), Tooltip("Open graph")]
        void OpenGraph() {
            voiceOverData?.OpenStoryGraph();
        }
        
        [Button("Update Length", ButtonSizes.Small), HorizontalGroup("Buttons",  Width = 90),
         Tooltip("Updates event length in FMOD studio, to be the same as provided audio file length.")]
        public void UpdateEventLength() {
            voiceOverData?.UpdateEventLength();
        }
        
        [Button("Fmod 🔍", ButtonSizes.Small), HorizontalGroup("Buttons", Width = 60), Tooltip("Show event in FMOD studio")]
        void ShowEventInFMODStudio() {
            voiceOverData?.ShowEventInFMODStudio();
        }

    }
}