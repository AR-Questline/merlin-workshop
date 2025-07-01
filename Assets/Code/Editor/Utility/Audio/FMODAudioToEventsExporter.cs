using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Awaken.TG.Editor.Localizations;
using Awaken.TG.Editor.SimpleTools;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Core;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using FMOD;
using FMODUnity;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using Thread = System.Threading.Thread;

namespace Awaken.TG.Editor.Utility.Audio {
    public static class FMODAudioToEventsExporter {
        const string Ip = "127.0.0.1";
        const int Port = 3663;

        static TelnetConnection s_telnetConnection;
        static string _lastSavedSelectedPath;

        static void StartNewTelnetConnection() {
            s_telnetConnection = new TelnetConnection(Ip, Port);

            if (!s_telnetConnection.connected) {
                EditorUtility.DisplayDialog("Error Connecting FMOD Studio", "Can't connect to FMOD Studio. Make sure FMOD Studio is open.", "Close");
            }
        }

        [MenuItem("TG/Audio/Generate VoiceOvers CSV for FMOD")]
        public static void GenerateVoiceOversCSVforFMOD() {
            ExportEventsToCSV(EditorAudioUtils.GetAllVoiceOverPaths());
        }
        
        [MenuItem("Assets/Generate VoiceOvers CSV for FMOD from selected graphs", priority = 99990), MenuItem("TG/Audio/Generate VoiceOvers CSV for FMOD from selected graphs")]
        public static void GenerateVoiceOversCSVforFMODFromGraphsSelected() {
            var graphs = Selection.objects.OfType<StoryGraph>();
            var filePaths = EditorAudioUtils.GetAllAudioFilePathsFromGraphs(graphs);
            ExportEventsToCSV(filePaths);
        }
        
        [MenuItem("Assets/Generate VoiceOvers CSV for FMOD from selected graphs", true)]
        public static bool IsSelectedStoryGraph() { 
            return Selection.objects.OfType<StoryGraph>().Any();
        }

        public static void UpdateEventLength(string audioFilePath) {
            StartNewTelnetConnection();

            var lines = new List<string>();

            string eventName = Path.GetFileNameWithoutExtension(audioFilePath);
            string eventNameWithExtension = Path.GetFileName(audioFilePath);
            lines.Add("var eventName = " + "'" + eventName + "';");
            lines.Add("var eventNameWithExtension = " + "'VoiceOvers/" + eventNameWithExtension + "';");
            lines.Add("var events = studio.project.model.Event.findInstances();");
            lines.Add("var event = events.filter(function(a) { return a.name == eventName; })[0];");
            lines.Add("var trackGroup = event.groupTracks[0];");
            lines.Add("var track = trackGroup.modules.find(function(m) {return m.name == eventName; });");
            lines.Add("var audio = studio.project.importAudioFile(eventNameWithExtension);");
            lines.Add("audio.refreshAudio();");
            lines.Add("track.length = audio.length;");

            // --- send data over tcp
            foreach (string line in lines) {
                s_telnetConnection.WriteLine(line);
                Sleep(10);
            }

            lines.Clear();
        }

        public static void RemoveEvents(IEnumerable<string> audioFilePaths) {
            StartNewTelnetConnection();
            foreach (var audioFilePath in audioFilePaths) {
                var lines = new List<string>();
                string eventName = Path.GetFileNameWithoutExtension(audioFilePath);
                string eventNameWithExtension = Path.GetFileName(audioFilePath);
                lines.Add("var eventName = " + "'" + eventName + "';");
                lines.Add("var eventNameWithExtension = " + "'VoiceOvers/" + eventNameWithExtension + "';");
                lines.Add("var events = studio.project.model.Event.findInstances();");
                lines.Add("var event = events.filter(function(a) { return a.name == eventName; })[0];");
                lines.Add("studio.project.deleteObject(event)");

                // --- send data over tcp
                foreach (string line in lines) {
                    s_telnetConnection.WriteLine(line);
                    Sleep(10);
                }

                lines.Clear();
            }

            Sleep(10);
            s_telnetConnection.WriteLine("studio.project.build();");
        }

        static void CreateEvents(IEnumerable<string> filePaths) {
            float count = filePaths.Count();
            int index = 0;
            using var progressBar = ProgressBar.Create("Creating VO Events");
            progressBar.Display(0, $"Events created: 0/{count}");
            foreach (string filePath in filePaths) {
                var lines = new List<string>();

                lines.Add("var path = " + "'" + filePath.Replace(@"\", @"/") + "'" + ";");
                lines.Add("var asset = studio.project.importAudioFile(path);");

                string eventPath = filePath.Replace(@"\", @"/");
                eventPath = Path.GetFileNameWithoutExtension(eventPath);

                // --- create & setup event
                lines.Add("var eventPath = " + "'" + eventPath + "';");
                lines.Add("var events = studio.project.model.Event.findInstances(); " +
                          "if (events.filter(function(a) { return a.name == eventPath; }).length > 0 == false) { " +
                          "var event = studio.project.workspace.addEvent('Event', true); " +
                          "event.name = eventPath; " +
                          "var track = event.addGroupTrack(); " +
                          "var sound = track.addSound(event.timeline, 'SingleSound', 0, 10); " +
                          "sound.audioFile = asset; " +
                          "sound.length = asset.length; " +
                          "sound.name = " + "'" + eventPath + "'; " +
                          "var bank = studio.project.lookup('bank:/VoiceOvers');" +
                          "event.relationships.banks.add(bank);" +
                          "}");

                // --- create VoiceOvers Directory
                lines.Add("var folders = studio.project.model.EventFolder.findInstances(); " +
                          "if (folders.filter(function(a) { return a.name == 'VoiceOvers'; }).length > 0 == false) " +
                          "{ var folder = studio.project.create('EventFolder'); folder.name = 'VoiceOvers'; event.folder = folder; }" +
                          " else { var folder = folders.filter(function(a) {return a.name == 'VoiceOvers';});" +
                          " var f = folder[0];" +
                          " event.folder = f; }");

                // --- send data over tcp
                foreach (string line in lines) {
                    s_telnetConnection.WriteLine(line);
                    Sleep(10);
                }

                lines.Clear();

                UpdateEventLength(filePath);

                index++;
                progressBar.Display(index / count, $"Events created: {index}/{count}");
            }

            Sleep(10);
        }

        public static void ExportEventsToCSV(IEnumerable<string> filePaths) {
            var pathToSave = EditorUtility.SaveFilePanel("Choose save file location", _lastSavedSelectedPath, "VoiceOversData", "csv");
            _lastSavedSelectedPath = pathToSave;
            using var stream = File.OpenWrite(pathToSave);
            using var writer = new StreamWriter(stream);
            // using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            // csv.Context.RegisterClassMap<VoiceOverToExportMap>();
            //
            // List<VoiceOverToExport> groupedData = new();
            // foreach (var filePath in filePaths) {
            //     string validFilePath = filePath.Replace(@"\", @"/");
            //     // --- Event
            //     string eventName = Path.GetFileNameWithoutExtension(validFilePath);
            //     if (groupedData.Any(g => g.eventName == eventName || g.filePath == validFilePath)) {
            //         Log.Important?.Error($"Multiple events with same name or file path! EventName: {eventName}! FilePath: {validFilePath}");
            //     }
            //
            //     // --- Graph
            //     string graph = eventName.Split('=').FirstOrDefault() ?? string.Empty;
            //     // --- Actor
            //     string id = eventName.Replace(EditorAudioUtils.VoiceOverIdSeparator, '/');
            //     TableEntry tableEntry = LocalizationHelper.GetTableEntry(id, LocalizationSettings.ProjectLocale);
            //     string actor = tableEntry?.GetMetadata<ActorMetaData>()?.ActorName ?? "None";
            //
            //     groupedData.Add(new VoiceOverToExport
            //         { filePath = validFilePath, eventName = eventName, storyGraph = graph, actor = actor });
            // }
            //
            // csv.WriteRecords(groupedData);
        }

        public static void Sleep(double msec) {
            for (DateTime since = DateTime.Now; (DateTime.Now - since).TotalMilliseconds < msec;) {
                Thread.Sleep(TimeSpan.FromTicks(10));
            }
        }

        // === Helper Classes
        // [UsedImplicitly]
        // sealed class VoiceOverToExportMap : ClassMap<VoiceOverToExport> {
        //     public VoiceOverToExportMap() {
        //         Map(m => m.filePath).Index(0).Name("FilePath");
        //         Map(m => m.eventName).Index(1).Name("Event");
        //         Map(m => m.storyGraph).Index(2).Name("Graph");
        //         Map(m => m.actor).Index(3).Name("Actor");
        //     }
        // }

        struct VoiceOverToExport {
            public string filePath;
            public string eventName;
            public string storyGraph;
            public string actor;
        }
    }
}