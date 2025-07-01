using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Awaken.TG.Editor.Localizations;
using Awaken.TG.Editor.Utility.Assets;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Editor;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Stories {
    [CustomEditor(typeof(StoryGraph)), CanEditMultipleObjects]
    public class StoryGraphInspectorEditor : OdinEditor {
        StoryGraph Graph => (StoryGraph) target;
        string _textToApply = string.Empty;
        
        public override void OnInspectorGUI() {
            DrawTemplateType();

            if (serializedObject.isEditingMultipleObjects) {
                EditorGUILayout.HelpBox("Cannot edit multiple objects", MessageType.Info);
                return;
            }
            
            var oldEnabled = GUI.enabled;
            GUI.enabled = oldEnabled && Application.isPlaying;
            
            if (GUILayout.Button("Start Story")) {
                StartStory(Graph);
            }
            if (GUILayout.Button("End Story")) {
                EndStory(Graph);
            }
            GUI.enabled = oldEnabled;

            EditorGUILayout.LabelField("Localization Prefix:", Graph.LocalizationPrefix);
            EditorGUILayout.BeginHorizontal();
            _textToApply = EditorGUILayout.TextArea(_textToApply, EditorStyles.textArea);
            if (GUILayout.Button("Apply New Label (this will take some time)")) {
                ApplyLabel(_textToApply);
            }
            
            EditorGUILayout.EndHorizontal();
            base.OnInspectorGUI();
        }

        void DrawTemplateType() {
            GUIUtils.PushLabelWidth(100);
            GUIUtils.PushFieldWidth(100);
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("templateType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(StoryGraph.hiddenInToolWindows)), new GUIContent("Hide in"));
            if (EditorGUI.EndChangeCheck()) {
                serializedObject.ApplyModifiedProperties();
                for (var i = 0; i < targets.Length; i++) {
                    EditorUtility.SetDirty(targets[i]);
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
            GUIUtils.PopFieldWidth();
            GUIUtils.PopLabelWidth();
        }

        // === Methods
        void ApplyLabel(string textToApply) {
            Graph.LocalizationPrefix = textToApply;
            UpdateGraphLocalizationTerms();
            _textToApply = string.Empty;
            EditorUtility.SetDirty(Graph.StringTable);
            EditorUtility.SetDirty(Graph);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        void UpdateGraphLocalizationTerms() {
            List<NodeElement> elements = new();
            foreach (StoryNode storyNode in Graph.nodes.OfType<StoryNode>()) {
                elements.AddRange(storyNode.elements);
            }
            foreach (var step in elements) {
                foreach (var loc in LocalizationUtils.GetLocalizedProperties(step)) {
                    LocalizationUtils.RenameTerm(Graph, loc.FieldPath, loc.LocProperty, step);
                }
            }
        }
        
        public static void StartStory(StoryGraph storyGraph) {
            StoryGraph graph = TemplatesUtil.Load<StoryGraph>(AssetsUtils.ObjectToGuid(storyGraph));

            Hero hero = TryExtractHero();
            Location place = TryExtractPlace(graph);

            Story.StartStory(StoryConfig.Location(place, StoryBookmark.EDITOR_ToInitialChapter(graph), typeof(VDialogue)));
        }

        public static void EndStory(StoryGraph storyGraph) {
            StoryGraph graph = TemplatesUtil.Load<StoryGraph>(AssetsUtils.ObjectToGuid(storyGraph));
            Story story = World.All<Story>().FirstOrDefault(s => s.EDITOR_Graph == graph);
            if (story != null) {
                StoryUtils.EndStory(story);
            }
        }

        static Hero TryExtractHero() {
            return Hero.Current;
        }

        static Location TryExtractPlace(StoryGraph graph) {
            return World.All<Location>().FirstOrDefault(loc => loc.TryGetElement<DialogueAction>()?.Bookmark?.EDITOR_Graph == graph);
        }
        
        static char delimiter = ',';

        void Export() {
            string filePath = EditorUtility.SaveFilePanel("Choose name",
                Path.GetDirectoryName(AssetDatabase.GetAssetPath(Graph)), Graph.name, "csv");
            if (string.IsNullOrWhiteSpace(filePath)) return;

            // Add SO GUID as first row
            var csv = new StringBuilder();
            csv.AppendLine($"{AssetsUtils.ObjectToGuid(Graph)},");

            List<EditorStep> steps = new List<EditorStep>();
            var graphNodes = Graph.nodes.OfType<ChapterEditorNode>();
            graphNodes.ForEach(c =>
                c.Steps.Where(s => s is SEditorText || s is SEditorChoice).ForEach(s => steps.Add((EditorStep) s)));
            var stepsDistinct = steps.Distinct();
            foreach (EditorStep step in stepsDistinct) {
                string text = string.Empty;
                if (step is SEditorText sText) {
                    text = sText.text;
                } else if (step is SEditorChoice sChoice) {
                    text = sChoice.choice.text;
                }

                // Replace single quote with double (csv needs this)
                text = Regex.Replace(text, "[“”\"]", "\"\"").TrimEnd();
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(step, out _, out long fileId);
                csv.AppendLine($"{fileId}{delimiter}\"{text}\"");
            }

            File.WriteAllText(filePath, csv.ToString());
        }

        void Import() {
            string filePath = EditorUtility.OpenFilePanel("Choose file",
                Path.GetDirectoryName(AssetDatabase.GetAssetPath(Graph)), "csv");
            if (string.IsNullOrWhiteSpace(filePath)) return;

            List<EditorStep> steps = new List<EditorStep>();
            var graphNodes = Graph.nodes.OfType<ChapterEditorNode>();
            graphNodes.ForEach(c =>
                c.Steps.Where(s => s is SEditorText || s is SEditorChoice).ForEach(s => steps.Add((EditorStep) s)));

            var sr = new StreamReader(filePath);
            var guid = sr.ReadLine()?.Replace(delimiter.ToString(), "");
            if (guid != AssetsUtils.ObjectToGuid(Graph)) return;
            sr.Close();

            // var dt = DataTable.New.Read(filePath);
            //
            // foreach (Row row in dt.Rows) {
            //     if (row.Values.Count <= 1) continue;
            //     var match = steps.Distinct().FirstOrDefault(s => {
            //         AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s, out _, out long fileId);
            //         return fileId.ToString() == row.Values[0];
            //     });
            //     if (match != null) {
            //         if (match is SEditorText sText) {
            //             sText.text = (LocString)row.Values[1];
            //         } else if (match is SEditorChoice sChoice) {
            //             sChoice.choice.text = (LocString)row.Values[1];
            //         }
            //     }
            // }
        }
    }
}
