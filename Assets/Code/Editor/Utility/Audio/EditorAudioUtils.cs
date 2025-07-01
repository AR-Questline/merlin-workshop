using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.Editor.Utility.StoryGraphs.Converter;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.TG.Main.Stories.Steps;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.Audio {
    public static class EditorAudioUtils {
        public const char VoiceOverIdSeparator = '=';
        
        public static string VoiceOverFilePath(SEditorText sEditorText) {
            string path = VoiceOverDirectory() + "/";
            path += sEditorText.text.ID.Replace('/', VoiceOverIdSeparator) + ".wav";
            return path;
        }

        public static string VoiceOverDirectory() {
            string path = Application.dataPath;
            path = path.Remove(path.IndexOf("Assets", StringComparison.Ordinal));
            path += "FMod/TG-RPG/Assets/VoiceOvers";
            return path;
        }

        public static List<string> GetAllVoiceOverPaths() {
            string voiceOverDirectory = VoiceOverDirectory();
            return GetAllAudioFilePathsFromDirectory(voiceOverDirectory);
        }
        
        public static List<string> GetAllAudioFilePathsFromDirectory(string directoryPath) {
            var directories = new List<string>(Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories));
            directories.Add(directoryPath);

            var filePaths = new List<string>();
            foreach (string directory in directories) {
                var paths = Directory.GetFiles(directory, "*.*",
                    SearchOption.TopDirectoryOnly).Where(s => s.ToLower().EndsWith(".wav"));
                filePaths.AddRange(paths);
            }

            return filePaths;
        }
        
        public static List<string> GetAllAudioFilePathsFromGraphs(IEnumerable<StoryGraph> graphs) {
            var audioFilePaths = new List<string>();
            foreach(var graph in graphs) {
                var sTexts = GraphConverterUtils.ExtractNodes<ChapterEditorNode>(graph).ExtractElements<ChapterEditorNode, SEditorText>().Select(trio => trio.element);
                foreach (var sText in sTexts) {
                    string audioFilePath = VoiceOverFilePath(sText);
                    audioFilePaths.Add(audioFilePath);
                }
            }

            return audioFilePaths;
        }

        public static string GetAudioFilePath(this SEditorText sEditorText) {
            string audioFilePath = VoiceOverFilePath(sEditorText);
            return audioFilePath;
        }
        
        public static string VoiceOverEventFromPath(string audioFilePath) {
            string id = Path.GetFileNameWithoutExtension(audioFilePath);
            return $"event:/VoiceOvers/{id}";
        }

        public static int GetEventLength(string audioFilePath) {
            // var eventRef = EventManager.Events.FirstOrDefault(e => e.Path == audioFilePath);
            // return eventRef == null ? 0 : eventRef.Length;
            return 0;
        }

        public static void GetGuidAndFileIdFromAudioFileId(string audioFileId, out string guid, out long fileId) {
            string[] ids = audioFileId.Split('_');
            guid = ids.Last();
            long.TryParse(ids.Reverse().Skip(1).Take(1).First(), out fileId);
        }
    }
}