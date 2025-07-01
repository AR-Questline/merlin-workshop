using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.SocialServices {
    public class SteamLocalizedStringsWindow : OdinEditorWindow {
        [Sirenix.OdinInspector.FilePath(AbsolutePath = true, Extensions = "vdf")]
        public string inputVdfPath;

        [Sirenix.OdinInspector.FilePath(AbsolutePath = true, Extensions = "csv")]
        public string inputTranslationsPath;

        [MenuItem("TG/Social Services/Steam Achievements Locales")]
        public static void OpenWindow() {
            var window = GetWindow<SteamLocalizedStringsWindow>();
            window.titleContent = new GUIContent("Achievement Localizations Editor");
        }

        [Button]
        void Localize() {
            var localizedTexts = AchievementLocalizationsUtil.ImportLocalizations(inputTranslationsPath);

            // Read the VDF file
            var valueById = ParseVdfTokens(inputVdfPath);
            
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("\"lang\"");
            stringBuilder.AppendLine("{");
            
            LocalizeLanguage("japanese", stringBuilder, valueById, localizedTexts, s => s.ja);
            LocalizeLanguage("italian", stringBuilder, valueById, localizedTexts, s => s.it);
            LocalizeLanguage("german", stringBuilder, valueById, localizedTexts, s => s.de);
            LocalizeLanguage("french", stringBuilder, valueById, localizedTexts, s => s.fr);
            LocalizeLanguage("schinese", stringBuilder, valueById, localizedTexts, s => s.cns);
            LocalizeLanguage("tchinese", stringBuilder, valueById, localizedTexts, s => s.cnt);
            LocalizeLanguage("czech", stringBuilder, valueById, localizedTexts, s => s.cz);
            LocalizeLanguage("spanish", stringBuilder, valueById, localizedTexts, s => s.es);
            LocalizeLanguage("brazilian", stringBuilder, valueById, localizedTexts, s => s.pt);
            LocalizeLanguage("russian", stringBuilder, valueById, localizedTexts, s => s.ru);
            LocalizeLanguage("polish", stringBuilder, valueById, localizedTexts, s => s.pl);
            
            stringBuilder.AppendLine("}");
            
            var outputPath = Path.Combine(Path.GetDirectoryName(inputVdfPath)!, "Output.vdf");
            File.WriteAllText(outputPath, stringBuilder.ToString());
        }

        void LocalizeLanguage(string locale, StringBuilder builder, Dictionary<string, string> valueById, 
            List<LocalizedStrings> localizedTexts, Func<LocalizedStrings, string> func) {
            
            builder.AppendLine($"\t\"{locale}\"");
            builder.AppendLine("\t{");
            builder.AppendLine("\t\t\"Tokens\"");
            builder.AppendLine("\t\t{");
            
            foreach (var kvp in valueById) {
                string id = kvp.Key;
                string text = kvp.Value;
                
                var localizedText = localizedTexts.FirstOrDefault(t => t.eng == text);
                if (localizedText != null) {
                    builder.AppendLine($"\t\t\t\"{id}\" \"{func(localizedText)}\"");
                } else {
                    Debug.LogError($"Couldn't find localized text for: {id} - {text}");
                }
            }
            
            builder.AppendLine("\t\t}");
            builder.AppendLine("\t}");
        }

        static Dictionary<string, string> ParseVdfTokens(string filePath) {
            var tokens = new Dictionary<string, string>();
            bool inTokensSection = false;
            var tokenPattern = new Regex(@"^\s*""(?<key>[^""]+)""\s+""(?<value>[^""]+)""\s*$");

            foreach (var line in File.ReadLines(filePath)) {
                if (line.Contains("\"Tokens\"")) {
                    inTokensSection = true;
                    continue;
                }

                if (inTokensSection && line.Trim() == "}") {
                    break; // End of "Tokens" section
                }

                if (inTokensSection) {
                    var match = tokenPattern.Match(line);
                    if (match.Success) {
                        string key = match.Groups["key"].Value;
                        string value = match.Groups["value"].Value;
                        tokens[key] = value;
                    }
                }
            }

            return tokens;
        }
    }
}