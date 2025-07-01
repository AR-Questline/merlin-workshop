using System.IO;
using Awaken.TG.Editor.Localizations;
using Awaken.TG.Editor.Main.Stories.Drawers;
using Awaken.TG.Editor.Utility.Audio;
using Awaken.TG.Editor.Utility.StoryGraphs;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps;
using Awaken.Utility;
using Awaken.Utility.Editor;
using FMOD;
using FMODUnity;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using EventReference = FMODUnity.EventReference;

namespace Awaken.TG.Editor.Main.Stories.Steps {
    [CustomElementEditor(typeof(SEditorText))]
    public class STextEditor : ElementEditor {
        SEditorText SEditorText => Target<SEditorText>();
        TableEntry TableEntry => TableEntryStatic(SEditorText);
        static TableEntry TableEntryStatic(SEditorText sEditorText) => LocalizationHelper.GetTableEntry(sEditorText.text.ID, LocalizationSettings.ProjectLocale);
        bool _actorChanged, _commentChanged, _foldout;

        protected override void OnStartGUI() {
            if (SEditorText.JustPasted && TableEntry != null) {
                UpdateSTextActorMetaData(SEditorText);
                UpdateComment(SEditorText);
                SEditorText.JustPasted = false;
            }
        }

        protected override void OnElementGUI() {
            int nodeWidth = NodeGUIUtil.GetNodeWidth(SEditorText.Parent);
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            float originalFieldWidth = EditorGUIUtility.fieldWidth;

            // --- we need to delay actorRef change by one frame cause it's path is uninitialized when EndChangeCheck is triggered
            if (_actorChanged) {
                UpdateActorMetaDataAndSetTablesDirty(SEditorText, TableEntry);
                _actorChanged = false;
            }

            // --- same for comment
            if (_commentChanged) {
                UpdateComment(SEditorText);
                _commentChanged = false;
            }
            
            if (SEditorText is SEditorTextCq) {
                DrawProperties(nameof(SEditorTextCq.iconRef));
                GUILayout.Space(3);
            }

            EditorGUILayout.BeginVertical();
            GUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.fieldWidth = 60;
            EditorGUI.BeginChangeCheck();
            DrawProperties(nameof(SEditorText.actorRef));
            if (EditorGUI.EndChangeCheck()) {
                _actorChanged = true;
            }

            EditorGUILayout.LabelField("→", GUILayout.Width(15));
            DrawProperties(nameof(SEditorText.targetActorRef));
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.fieldWidth = originalFieldWidth;

            GUILayout.Space(3);

            GUIUtils.PushLabelWidth(150);
            DrawProperties(nameof(SEditorText.lookAtOnlyWithHead));
            GUIUtils.PopLabelWidth();

            GUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(-0.5f);
            EditorGUI.BeginChangeCheck();
            DrawProperties("text");
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(3);

            DrawProperties("gestureKey");

            GUILayout.Space(3);

            DrawTextCounter(SEditorText.textLength, SEditorText.MaxCharsPerLine, nodeWidth);
            EditorGUILayout.EndVertical();

            GUILayout.Space(-20);
            DrawProperties("hasVoice");

            if (SEditorText.hasVoice) {
                _foldout = EditorGUILayout.Foldout(_foldout, "Data");
                if (_foldout) {
                    EditorGUILayout.BeginHorizontal();
                    DrawProperties("overrideDuration");
                    EditorGUILayout.EndHorizontal();
                    if (SEditorText.overrideDuration) {
                        DrawProperties("cutDurationMilliseconds");
                    }

                    EditorGUILayout.BeginVertical();
                    EditorGUIUtility.labelWidth = 60;
                    DrawProperties("audioClip");

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUIUtility.labelWidth = 85;
                    SEditorText.waitForInput = EditorGUILayout.Toggle("WaitForInput:", SEditorText.waitForInput, GUILayout.Width(105));
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(-20);
                    EditorGUI.BeginChangeCheck();
                    DrawProperties("commentInfo");
                    if (EditorGUI.EndChangeCheck()) {
                        _commentChanged = true;
                    }

                    DrawProperties("techInfo");
                    EditorGUILayout.EndVertical();
                }

                SetupAudio(SEditorText, false);
            }

            DrawProperties("emotions");

            EditorGUIUtility.labelWidth = originalLabelWidth;

            if (SEditorText.Parent.Graph.AutofillActors) {
                AutofillActors();
            }
        }

        void UpdateComment(SEditorText sEditorText) {
            var tableEntryMetas = TableEntry.SharedEntry.Metadata;
            if (string.IsNullOrWhiteSpace(sEditorText.commentInfo)) {
                foreach (var description in tableEntryMetas.GetMetadatas<GestureMetadata>()) {
                    tableEntryMetas.RemoveMetadata(description);
                }
            } else {
                GestureMetadata gestureMetadata = tableEntryMetas.GetMetadata<GestureMetadata>();
                if (gestureMetadata != null) {
                    gestureMetadata.GestureKey = sEditorText.commentInfo;
                } else {
                    tableEntryMetas.AddMetadata(new GestureMetadata(sEditorText.commentInfo));
                }
            }

            SetTableDirty(TableEntry);
        }

        static void UpdateActorMetaDataAndSetTablesDirty(SEditorText sEditorText, TableEntry tableEntry) {
            if (tableEntry == null) {
                return;
            }

            ActorMetaData actorMetaData = tableEntry.GetMetadata<ActorMetaData>();
            if (actorMetaData != null) {
                actorMetaData.ActorName = ActorsRegister.Get.Editor_GetActorName(sEditorText.actorRef);
            } else {
                tableEntry.AddMetadata(new ActorMetaData(ActorsRegister.Get.Editor_GetActorName(sEditorText.actorRef)));
            }

            SetTableDirty(tableEntry);
        }

        void AutofillActors() {
            var talker = SEditorText.genericParent.Graph.allowedActors[0];

            var talkerProp = _serializedObject.FindProperty(nameof(SEditorText.actorRef));
            var talkerGuidProp = talkerProp.FindPropertyRelative(nameof(ActorRef.guid));
            var talkerGuid = talkerGuidProp.stringValue;
            if (ActorRefUtils.IsNoneGuid(talkerGuid)) {
                talkerGuidProp.stringValue = talker.guid;
                UpdateActorMetaDataAndSetTablesDirty(SEditorText, TableEntry);
            }

            var listenerProp = _serializedObject.FindProperty(nameof(SEditorText.targetActorRef));
            var listenerGuidProp = listenerProp.FindPropertyRelative(nameof(ActorRef.guid));
            var listenerGuid = listenerGuidProp.stringValue;
            if (ActorRefUtils.IsNoneGuid(listenerGuid)) {
                listenerGuidProp.stringValue = DefinedActor.Hero.ActorGuid;
            }
        }

        static void SetTableDirty(TableEntry tableEntry) {
            var tableCollection = LocalizationEditorSettings.GetStringTableCollection(tableEntry.Table.TableCollectionName);
            EditorUtility.SetDirty(tableEntry.Table);
            EditorUtility.SetDirty(tableCollection);
            EditorUtility.SetDirty(tableCollection.SharedData);
        }

        public static void SetupAudio(SEditorText sEditorText, bool forceGeneration, bool findEventReference = false, bool forceIdUpdate = false) {
            bool emptyText = sEditorText.text == null || string.IsNullOrWhiteSpace(sEditorText.text.ID) || string.IsNullOrWhiteSpace(sEditorText.text.ToString());
            if (emptyText) {
                return;
            }

            string audioFilePath = EditorAudioUtils.VoiceOverFilePath(sEditorText);

            if (!sEditorText.hasVoice) {
                if (File.Exists(audioFilePath)) {
                    File.Delete(audioFilePath);
                }

                return;
            }

            bool hasOverride = !string.IsNullOrWhiteSpace(TableEntryStatic(sEditorText).GetMetadata<AudioReplacementName>()?.AudioReplacement);
            if (!File.Exists(audioFilePath)) {
                WaveFileGenerator.CreateAndSave(audioFilePath);
            } else if (forceGeneration && !hasOverride) {
                File.Delete(audioFilePath);
                WaveFileGenerator.CreateAndSave(audioFilePath);
            }

            if (findEventReference) {
                string id = Path.GetFileNameWithoutExtension(audioFilePath);
                // if (forceIdUpdate || sEditorText.audioClip.Guid.IsNull || !sEditorText.audioClip.Path.Contains(id) ||
                //     EditorUtils.System.getEventByID(sEditorText.audioClip.Guid, out _) != RESULT.OK) {
                //     var eventRef = EventManager.EventFromEventName(id);
                //     if (eventRef != null && !eventRef.Guid.IsNull) {
                //         sEditorText.audioClip = new EventReference { Path = eventRef.Path, Guid = eventRef.Guid };
                //     }
                // }
            }
        }

        public static void UpdateSTextActorMetaData(SEditorText sEditorText) {
            UpdateActorMetaDataAndSetTablesDirty(sEditorText, TableEntryStatic(sEditorText));
        }

        /// <summary>
        /// Creates a new SEditorText node with the specified phrase, assigning the given speaker and listener.
        /// The phrase is added to the string table, and the actor metadata is updated accordingly.
        /// It marks the graph and string table as dirty, but saving the asset is required.
        /// </summary>
        /// <param name="chapter">The node to which the new SEditorText NodeElement will be attached.</param>
        /// <param name="speaker">The actor delivering the line.</param>
        /// <param name="listener">The actor receiving the line.</param>
        /// <param name="phrase">The spoken phrase.</param>
        public static void CreateSText(ChapterEditorNode chapter, ActorRef speaker, ActorRef listener, string phrase) {
            StoryGraph graph = chapter.Graph;
            SEditorText sText = (SEditorText)StoryNodeEditor.CreateElement(chapter, typeof(SEditorText));
            var locTable = graph.StringTable;
            
            // assign actors
            sText.actorRef = speaker;
            sText.targetActorRef = listener;

            // assign phrase 
            string newTerm = GetNewLocTextId(sText, graph);
            sText.text.ID = newTerm;
            var entry = locTable.AddEntry(sText.text.ID, phrase);
            
            // update actor metadata (for VoiceOvers assignment tools)
            UpdateActorMetaDataAndSetTablesDirty(sText, entry);
            
            // marking graph as dirty, marking the string table is not necessary as UpdateActorMetaDataAndSetTablesDirty already does it.
            EditorUtility.SetDirty(graph);
        }
        
        /// <summary>
        /// Updates the phrase of the given SEditorText node, updating the string table accordingly.
        /// It marks the string table as dirty, but saving the asset is required.
        /// Marking the graph as dirty is not necessary, as the string table is a separate asset.
        /// </summary>
        /// <param name="sText">The sText node element to be updated.</param>
        /// <param name="phrase">The spoken phrase.</param>
        public static void UpdateSText(SEditorText sText, string phrase) {
            // update phrase 
            var graph = sText.Parent.Graph;
            var tableEntry = graph.StringTable.GetEntry(sText.text.ID);
            tableEntry.Value = phrase;
            
            // mark table as dirty, making graph dirty is not necessary
            SetTableDirty(tableEntry);
        }

        /// <summary>
        /// Updates the phrase and actors of the given SEditorText node, updating the string table accordingly.
        /// It marks the graph and string table as dirty, but saving the asset is required.
        /// </summary>
        /// <param name="sText">The sText node element to be updated.</param>
        /// <param name="speaker">The actor delivering the line.</param>
        /// <param name="listener">The actor receiving the line.</param>
        /// <param name="phrase">The spoken phrase.</param>
        public static void UpdateSText(SEditorText sText, ActorRef speaker, ActorRef listener, string phrase) {
            // update phrase 
            var graph = sText.Parent.Graph;
            var locTable = graph.StringTable;
            var tableEntry = locTable.GetEntry(sText.text.ID);
            tableEntry.Value = phrase;

            // assign actors
            sText.actorRef = speaker;
            sText.targetActorRef = listener;
            
            // update actor metadata (for VoiceOvers assignment tools)
            UpdateActorMetaDataAndSetTablesDirty(sText, tableEntry);
            
            // marking graph as dirty, marking the string table is not necessary as UpdateActorMetaDataAndSetTablesDirty already does it.
            EditorUtility.SetDirty(graph);
        }
        
        static string GetNewLocTextId(SEditorText sText, StoryGraph graph) {
            SerializedObject serializedObject = new(sText);
            var locStringProperty = serializedObject.FindProperty(nameof(sText.text));
            string localizationPrefix = graph.LocalizationPrefix;
            LocalizationUtils.ValidateTerm(locStringProperty, localizationPrefix, out string newId);
            return newId;
        }
    }
}