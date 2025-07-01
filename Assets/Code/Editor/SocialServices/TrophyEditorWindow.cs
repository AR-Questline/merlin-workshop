using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.SocialServices {
    public class TrophyEditorWindow : OdinEditorWindow {
        [Sirenix.OdinInspector.FilePath(AbsolutePath = true, Extensions = "json")]
        public string inputJsonPath;

        [Sirenix.OdinInspector.FilePath(AbsolutePath = true, Extensions = "csv")]
        public string inputTranslationsPath;

        [MenuItem("TG/Social Services/Trophies Editor")]
        public static void OpenWindow() {
            var window = GetWindow<TrophyEditorWindow>();
            window.titleContent = new GUIContent("Trophy Editor");
        }

        [Button]
        void ImportTranslations() {
            var localizedTrophies = AchievementLocalizationsUtil.ImportLocalizations(inputTranslationsPath);
            Root trophiesRoot = LoadTrophies();

            foreach (var trophy in trophiesRoot.entities.trophies) {
                AssignLocalizedValues(trophy, localizedTrophies);
            }
            foreach (var trophy in trophiesRoot.entities.trophyGroups.SelectMany(g => g.links.trophies)) {
                AssignLocalizedValues(trophy.@object, localizedTrophies);
            }
            
            var jsonSettings = new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
            };
            var json = JsonConvert.SerializeObject(trophiesRoot, jsonSettings);
            
            var outputPath = Path.Combine(Path.GetDirectoryName(inputJsonPath)!, "Output.json");
            using var writer = new StreamWriter(outputPath);
            writer.Write(json);
            
            Debug.LogError("Trophies exported successfully.");
        }

        static void AssignLocalizedValues(Trophy trophy, List<LocalizedStrings> localizedTrophies) {
            if (string.IsNullOrEmpty(trophy.metadata?.name?.enUS)) {
                return;
            }
                
            var localizedTrophyName = localizedTrophies.FirstOrDefault(t => t.eng == trophy.metadata.name.enUS);
            if (localizedTrophyName != null) {
                trophy.metadata.name.frFR = localizedTrophyName.fr;
                trophy.metadata.name.deDE = localizedTrophyName.de;
                trophy.metadata.name.esES = localizedTrophyName.es;
                trophy.metadata.name.itIT = localizedTrophyName.it;
                trophy.metadata.name.jaJP = localizedTrophyName.ja;
                trophy.metadata.name.zhHans = localizedTrophyName.cns;
                trophy.metadata.name.zhHant = localizedTrophyName.cnt;
                trophy.metadata.name.csCZ = localizedTrophyName.cz;
                trophy.metadata.name.plPL = localizedTrophyName.pl;
                trophy.metadata.name.ptBR = localizedTrophyName.pt;
                trophy.metadata.name.ruRU = localizedTrophyName.ru;
            } else {
                Debug.LogError($"Haven't found localization for {trophy.metadata.name.enUS}");
            }
                
            var localizedTrophyDesc = localizedTrophies.FirstOrDefault(t => t.eng == trophy.metadata.description.enUS);
            if (localizedTrophyDesc != null) {
                trophy.metadata.description.frFR = localizedTrophyDesc.fr;
                trophy.metadata.description.deDE = localizedTrophyDesc.de;
                trophy.metadata.description.esES = localizedTrophyDesc.es;
                trophy.metadata.description.itIT = localizedTrophyDesc.it;
                trophy.metadata.description.jaJP = localizedTrophyDesc.ja;
                trophy.metadata.description.zhHans = localizedTrophyDesc.cns;
                trophy.metadata.description.zhHant = localizedTrophyDesc.cnt;
                trophy.metadata.description.csCZ = localizedTrophyDesc.cz;
                trophy.metadata.description.plPL = localizedTrophyDesc.pl;
                trophy.metadata.description.ptBR = localizedTrophyDesc.pt;
                trophy.metadata.description.ruRU = localizedTrophyDesc.ru;
            } else {
                Debug.LogError($"Haven't found localization for {trophy.metadata?.description?.enUS}");
            }
        }

        Root LoadTrophies() {
            using var stream = File.OpenRead(Path.Combine(inputJsonPath));
            using var reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader);
            var serializer = new JsonSerializer();
            return serializer.Deserialize<Root>(jsonReader);
        }
    }
}