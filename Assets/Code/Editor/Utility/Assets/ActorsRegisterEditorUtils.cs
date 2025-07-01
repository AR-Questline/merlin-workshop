using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Awaken.TG.Main.Stories.Actors;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.Assets {
    public static class ActorsRegisterEditorUtils {
        static string _lastSavedSelectedPath;

        //[MenuItem("TG/Assets/Actors Register Utils/Generate Actors with Guids CSV")]
        static void GenerateActorsWithGuidsCSV() {
            var pathToSave = EditorUtility.SaveFilePanel("Choose save file location", _lastSavedSelectedPath, "VoiceOversData", "csv");
            _lastSavedSelectedPath = pathToSave;
            using var stream = File.OpenWrite(pathToSave);
            using var writer = new StreamWriter(stream);
            // using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            // csv.Context.RegisterClassMap<ActorGuidToExportMap>();
            //
            // List<ActorGuidToExport> groupedData = new();
            // foreach (var actor in ActorsRegister.Get.AllActors) {
            //     groupedData.Add(new ActorGuidToExport {
            //         guid = actor.Guid,
            //         name = ActorsRegister.Get.Editor_GetActorName(actor.Guid)
            //     });
            // }
            //
            // csv.WriteRecords(groupedData);
        }

        //[MenuItem("TG/Assets/Actors Register Utils/Change All Guids in Localizations to Actor Names")]
        static void ChangeAllGuidsInLocalizationsToActorNames() {
            string[] allAssets = Directory.GetFiles(Application.dataPath + "/Localizations", "*.asset*", SearchOption.AllDirectories);
            var actorTemps = ActorsRegister.Get.AllActors
                .Select(s => new ActorGuidToExport() { name = ActorsRegister.Get.Editor_GetActorName(s.Guid), guid = s.Guid }).ToArray();
            
            Parallel.For(0, allAssets.Length - 1, (i, state) => {
                var file = allAssets[i];
                string text = File.ReadAllText(file);
                bool isAnyChange = false;
                foreach (var actor in actorTemps) {
                    string name = actor.name;
                    if (name.Contains('?')) {
                        name = name.Replace("?", "\\?");
                    }

                    if (Regex.IsMatch(text, actor.guid)) {
                        text = Regex.Replace(text, actor.guid, name);
                        isAnyChange = true;
                    }
                }

                if (isAnyChange) {
                    File.WriteAllText(file, text);
                }
            });
        }

        // === Helper Classes
        // [UsedImplicitly]
        // sealed class ActorGuidToExportMap : ClassMap<ActorGuidToExport> {
        //     public ActorGuidToExportMap() {
        //         Map(m => m.guid).Index(0).Name("Guid");
        //         Map(m => m.name).Index(3).Name("Actor");
        //     }
        // }

        struct ActorGuidToExport {
            public string guid;
            public string name;
        }
    }
}