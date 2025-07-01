using System;
using System.Collections.Generic;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Core;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Localizations {
    public class StoryScriptExporterWindow : OdinEditorWindow {

        [HideLabel]
        public Settings settings;
        
        [MenuItem("TG/Localization/Story Script Exporter")]
        static void OpenWindow() {
            var window = OdinEditorWindow.GetWindow<StoryScriptExporterWindow>();
            window.maxSize = new Vector2(500, 400);
        }

        [Button][PropertyTooltip("Exports all possible dialogue outcomes, repeating many lines in the process.")]
        void ExportDialogues() {
            StoryScriptExporter.ExportAllGraphs(ScriptType.Dialogues, settings);
        }
        
        [Button][PropertyTooltip("Exports all texts from graphs in order, makes no duplicates.")]
        void ExportTexts() {
            StoryScriptExporter.ExportAllGraphs(ScriptType.Texts, settings);
        }
        
        [Button][PropertyTooltip("Exports all possible dialogue outcomes between choices.")]
        void ExportVOScript() {
            StoryScriptExporter.ExportAllGraphs(ScriptType.VoiceActors, settings);
        }
        
        [Serializable]
        public class Settings {
            public SelectionType SelectBy = SelectionType.Reference;
            [ShowIf("SelectBy", SelectionType.Directory),FolderPath(AbsolutePath = false)]
            public string[] directoryPaths = {
                "Assets/Data/Templates/Stories"
            };
            [ShowIf("SelectBy", SelectionType.Reference)]
            public List<StoryGraph> storyGraphs;
            
            [Tooltip("Filter by actor, leave 'None' if you want to export texts of all actors.")]
            public ActorRef actorFilter = DefinedActor.None.ActorRef;
            [Tooltip("Set true to create singular file for each actor, set false to get it all in one file.")]
            public bool separatePerActor;
            [Tooltip("Set true if you want export file containing only texts.")]
            public bool exportOnlyTexts;
            [Tooltip("Set true if you want to export only redacted texts.")]
            public bool exportOnlyRedacted;
            [Tooltip("Set true if you want exclude ALL duplicates per actor. If different actors say the same line, it will not be removed." +
                     "\n Use when you want to export only unique texts.")]
            public bool forceExclusionOfDuplicates;
            [Tooltip("This property is used only in \"Export VO Script\". Use HasVO to export lines with already valid VO.\nUse Missing to export lines without any VO.\nUse Mismatched to export lines with mismatched VO.")]
            public VOFilterType voFilter = VOFilterType.All;
            
            public enum SelectionType : byte {
                Directory,
                Reference
            }
        }
    }
    
    public enum ScriptType : byte {
        Texts = 0,
        Dialogues = 1,
        VoiceActors = 2,
    }
    
    [Flags]
    public enum VOFilterType : byte {
        HasVO = 1 << 0,
        Missing = 1 << 1,
        Mismatched = 1 << 2,
        MissingAndMismatched = Missing | Mismatched,
        All = HasVO | Missing | Mismatched
    }
}