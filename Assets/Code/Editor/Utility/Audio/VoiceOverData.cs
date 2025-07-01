using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Awaken.TG.Editor.Localizations;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.TimeLines.Markers;
using Awaken.TG.Main.Utility.Audio;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using FMOD.Studio;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.Timeline;
using XNodeEditor;
using EventInstance = FMOD.Studio.EventInstance;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Utility.Audio {
    public class VoiceOverData {
        static readonly OnDemandCache<StoryGraph, ScriptEntry[]> EntriesOnDemand =
            new(g => StoryScriptExporter.EnumerateOnlyOneStoryGraph(g, ScriptType.Texts).ToArray());

        public static void ResetCache() => EntriesOnDemand.Clear();

        [DisplayAsString(false)] public readonly string dialogueLine;

        [DisplayAsString(false), TableColumnWidth(120, Resizable = false)]
        public readonly string actor;

        AudioReplacementName _audioMeta;

        public string ID { get; }
        public string AudioFilePath { get; }
        public string StoryGraphPath { get; }
        public bool UsedInStory { get; }
        public StoryGraph StoryGraph { get; }
        TableEntry TableEntry { get; }
        EventInstance PreviewEventInstance { get; set; }
        public string AudioReplacementName => _audioMeta?.AudioReplacement ?? string.Empty;

        [ShowInInspector, DisplayAsString(false), LabelText(""), HorizontalGroup("AudioReplacement"),
         TableColumnWidth(300, Resizable = false)]
        public string DisplayedAudioName => $"{AudioReplacementName}{(UsedInStory ? string.Empty : "(Unused)")}";

        public bool VOMissingOrInvalid => !HasAudioReplacement || TranslationMismatch;
        public bool HasAudioReplacement => !string.IsNullOrWhiteSpace(AudioReplacementName);
        public bool MissingEntry => TableEntry == null;

        public bool IsPlaying {
            get {
                // PreviewEventInstance.getPlaybackState(out PLAYBACK_STATE state);
                // return state == PLAYBACK_STATE.PLAYING;
                return false;
            }
        }

        /// <summary>
        /// Means that VO was created on different version of this text
        /// </summary>
        [ShowInInspector, VerticalGroup("Mismatch"), TableColumnWidth(100, Resizable = false), LabelText(" "),
         GUIColor(nameof(MismatchColor))]
        [Tooltip("Means that VO was assigned on different version of this text and should be checked if it's still valid")]
        public bool TranslationMismatch => HasAudioReplacement && TableEntry.LocalizedValue.GetHashCode() != _audioMeta?.TranslationHash;

        public Color PlayStopColor => IsPlaying ? Color.red : Color.green;
        Color MismatchColor => TranslationMismatch ? Color.red : GUI.color;

        public VoiceOverData(string audioFilePath) {
            AudioFilePath = audioFilePath;
            ID = Path.GetFileNameWithoutExtension(audioFilePath)
                .Replace(EditorAudioUtils.VoiceOverIdSeparator, '/');
            TableEntry = LocalizationHelper.GetTableEntry(ID, LocalizationSettings.ProjectLocale);
            if (MissingEntry) {
                dialogueLine = $"<color=#FF0000>Missing Entry!</color>";
                return;
            }

            EditorAudioUtils.GetGuidAndFileIdFromAudioFileId(ID, out string guid, out _);
            StoryGraph = AssetDatabase.LoadAssetAtPath<StoryGraph>(AssetDatabase.GUIDToAssetPath(guid));
            StoryGraphPath = AssetDatabase.GetAssetPath(StoryGraph);

            if (StoryGraph == null) {
                UsedInStory = false;
                Log.Minor?.Warning("Wrong audio file path, probably missing story graph for " + audioFilePath);
            } else {
                UsedInStory = EntriesOnDemand[StoryGraph].Any(scriptEntry => scriptEntry.id == ID);
            }

            dialogueLine = LocalizationHelper.Translate(ID, LocalizationSettings.ProjectLocale);
            actor = TableEntry.GetMetadata<ActorMetaData>()?.ActorName ?? string.Empty;
            _audioMeta = TableEntry.GetMetadata<AudioReplacementName>();
        }

        public void AssignNewAudioReplacement(string newAudioPath) {
            if (!string.IsNullOrWhiteSpace(newAudioPath)) {
                File.Delete(AudioFilePath);
                File.Copy(newAudioPath, AudioFilePath);
                VoiceOversEditor.lastSearchedDirectory = Path.GetDirectoryName(newAudioPath);
                string replacementName = Path.GetFileName(newAudioPath);

                if (_audioMeta == null) {
                    _audioMeta = new AudioReplacementName();
                    TableEntry.AddMetadata(_audioMeta);
                }

                _audioMeta.AudioReplacement = replacementName;
                _audioMeta.TranslationHash = TableEntry.LocalizedValue.GetHashCode();

                SetTableEntryDirty();
                UpdateEventLength();
            }
        }

        [Button("Assign", ButtonSizes.Large), HorizontalGroup("AudioReplacement")]
        void AssignNewAudioReplacement() {
            if (MissingEntry) {
                return;
            }

            string newAudioClip =
                EditorUtility.OpenFilePanel("Choose audio file", VoiceOversEditor.lastSearchedDirectory, "WAV");
            AssignNewAudioReplacement(newAudioClip);
        }

        [Button("Clear", ButtonSizes.Large), HorizontalGroup("AudioReplacement")]
        public void ClearAudioReplacementMetaData() {
            if (MissingEntry) {
                return;
            }

            File.Delete(AudioFilePath);
            WaveFileGenerator.CreateAndSave(AudioFilePath);

            foreach (var audioReplacement in TableEntry.GetMetadatas<AudioReplacementName>()) {
                TableEntry.RemoveMetadata(audioReplacement);
            }

            _audioMeta = null;
            SetTableEntryDirty();

            // --- Send update to FMOD
            UpdateEventLength();
        }

        [Button("Resolve", ButtonSizes.Small), ShowIf(nameof(HasAudioReplacement)), ShowIf(nameof(TranslationMismatch)),
         Tooltip("Will mark this VO as correctly assigned to given text"), VerticalGroup("Mismatch")]
        public void ResolveMismatch() {
            _audioMeta.TranslationHash = TableEntry.LocalizedValue.GetHashCode();
            SetTableEntryDirty();
        }

        [Button("Open Graph", ButtonSizes.Small), VerticalGroup("Buttons"), TableColumnWidth(150, Resizable = false)]
        public void OpenStoryGraph() {
            if (MissingEntry) {
                return;
            }

            StoryNode node = GetStoryNode();
            NodeEditorWindow.Open(StoryGraph).CenterOnNode(node);
            Selection.objects = new Object[] { node };
        }

        [Button("Copy Id", ButtonSizes.Small), VerticalGroup("Buttons")]
        public void CopyIdToClipBoard() {
            int index = ID.IndexOf('/');
            GUIUtility.systemCopyBuffer = index < 0 ? ID : ID.Substring(index + 1, ID.Length - (index + 1));
        }

        [Button(SdfIconType.Play), GUIColor(nameof(PlayStopColor)), HideLabel, VerticalGroup("Buttons"), HideIf(nameof(IsPlaying))]
        public void PlayPreview() {
            if (MissingEntry) {
                return;
            }

            EditorAudioUtils.GetGuidAndFileIdFromAudioFileId(ID, out string guid, out long fileId);

            // if (TryFindMatchingEventRef(fileId.ToString(), guid, out var eventRef) == false) {
            //     Log.Debug?.Error($"Event with guid: {guid} and id {fileId.ToString()} not found in events cache");
            //     return;
            // }
            
            try {
                // EditorUtils.StopAllPreviews();
            } catch (Exception) {
                //do nothing, even if something breaks upon stopping all previews(e.g. modifying collection), there is always a possibility to stop a playing sound manually. 
            }

            // EditorUtils.LoadPreviewBanks();

            // PreviewEventInstance = EditorUtils.PreviewEvent(eventRef, null);
            // PreviewEventInstance.start();
        }

        [Button(SdfIconType.Stop), HideLabel, GUIColor(nameof(PlayStopColor)), VerticalGroup("Buttons"), ShowIf(nameof(IsPlaying))]
        public void StopPlayingPreview() {
            // PreviewEventInstance.stop(STOP_MODE.IMMEDIATE);
        }

        [Button("ShowEventInFMODStudio", ButtonSizes.Small), VerticalGroup("Buttons")]
        public void ShowEventInFMODStudio() {
            EditorAudioUtils.GetGuidAndFileIdFromAudioFileId(ID, out string guid, out long fileId);
            string idWithGuid = fileId + "_" + guid;

            // if (TryFindMatchingEventRef(idWithGuid, out var eventRef) == false) {
            //     Log.Debug?.Error($"Event with guid: {guid} and id {fileId.ToString()} not found in events cache");
            //     return;
            // }

            // string cmd = string.Format("studio.window.navigateTo(studio.project.lookup(\"{0}\"))", eventRef.Guid);
            // EditorUtils.SendScriptCommand(cmd);
        }

        [Button("UpdateEventLength", ButtonSizes.Small), VerticalGroup("Buttons"),
         Tooltip("Updates event length in FMOD studio, to be the same as provided audio file length.")]
        public void UpdateEventLength() {
            // --- Send update to FMOD
            FMODAudioToEventsExporter.UpdateEventLength(AudioFilePath);
        }

        public StoryNode GetStoryNode() {
            EditorAudioUtils.GetGuidAndFileIdFromAudioFileId(ID, out _, out long fileId);

            StoryNode node = StoryGraph.nodes.OfType<StoryNode>().FirstOrDefault(n => {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(n, out string _, out long localId);
                foreach (var element in n.elements) {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(element, out string _, out long elementId);
                    if (elementId == fileId) {
                        return true;
                    }
                }

                return localId == fileId;
            });
            return node;
        }

        [Button("Edit TimeLine", ButtonSizes.Small), VerticalGroup("Buttons")]
        void EditTimeLine() {
            if (MissingEntry) {
                return;
            }

            // RuntimeManager.WaitForAllSampleLoading();
            EditorAudioUtils.GetGuidAndFileIdFromAudioFileId(ID, out string guid, out long fileId);
            StoryGraph storyGraph = AssetDatabase.LoadAssetAtPath<StoryGraph>(AssetDatabase.GUIDToAssetPath(guid));
            NodeElement node = storyGraph.nodes.OfType<StoryNode>().SelectMany(n => n.elements).FirstOrDefault(e => {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(e, out string _, out long localId);
                return localId == fileId;
            });
            if (node is SEditorText sText) {
                TimelineAsset timeLine = EditorGUIUtility.Load("TimeLines/VoiceOverTimeLine.playable") as TimelineAsset;
                if (timeLine == null) {
                    return;
                }

                timeLine.GetRootTracks().ToList().ForEach(t => timeLine.DeleteTrack(t));
                timeLine.GetOutputTracks().ToList().ForEach(t => timeLine.DeleteTrack(t));

                // --- Fmod Event
                // FMODEventTrack eventTrack = timeLine.CreateTrack<FMODEventTrack>();
                // var clip = eventTrack.CreateClip<FMODEventPlayable>();
                // FMODEventPlayable playable = (FMODEventPlayable)clip.asset;
                // string path = EditorAudioUtils.VoiceOverEventFromPath(AudioFilePath);
                // playable.EventReference = EventReference.Find(path);
                // playable.UpdateEventDuration(EditorAudioUtils.GetEventLength(path));
                // if (playable.EventLength > 0) {
                //     clip.duration = playable.EventLength;
                //     timeLine.fixedDuration = playable.EventLength;
                // }
                //
                // // --- Emotion Markers
                // timeLine.CreateMarkerTrack();
                // foreach (var emotionData in sText.emotions) {
                //     EmotionMarker marker = timeLine.markerTrack.CreateMarker<EmotionMarker>(emotionData.startTime);
                //     marker.emotionKey = emotionData.emotionKey;
                //     marker.emotionState = emotionData.state;
                // }
                //
                // // --- Opening Editor
                // TimeLineEditor.Open(timeLine, sText);
            }
        }

        void SetTableEntryDirty() {
            var tableCollection =
                LocalizationEditorSettings.GetStringTableCollection(TableEntry.Table.TableCollectionName);
            EditorUtility.SetDirty(TableEntry.Table);
            EditorUtility.SetDirty(tableCollection);
            EditorUtility.SetDirty(tableCollection.SharedData);
        }
        
        // bool TryFindMatchingEventRef(string id, string guid, out EditorEventRef eventRef) {
        //     // var eventsRefs = EventManager.Events;
        //     eventRef = null;
        //     for (int i = 0; i < eventsRefs.Count; i++) {
        //         var e = eventsRefs[i];
        //         if (e == null) {
        //             continue;
        //         }
        //         if (Regex.IsMatch(e.Path, id) && Regex.IsMatch(e.Path, guid)) {
        //             eventRef = e;
        //             break;
        //         }
        //     }
        //     return eventRef != null;
        // }
   
        // bool TryFindMatchingEventRef(string idWithGuid, out EditorEventRef eventRef) {
        //     var eventsRefs = EventManager.Events;
        //     eventRef = null;
        //     for (int i = 0; i < eventsRefs.Count; i++) {
        //         var e = eventsRefs[i];
        //         if (e == null) {
        //             continue;
        //         }
        //         if (Regex.IsMatch(e.Path, idWithGuid)) {
        //             eventRef = e;
        //             break;
        //         }
        //     }
        //     return eventRef != null;
        // }
    }
}