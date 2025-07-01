using System.Linq;
using System.Xml;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.SocialServices {
    public class MicrosoftLocalizedStringsWindow : OdinEditorWindow {
        [Sirenix.OdinInspector.FilePath(AbsolutePath = true, Extensions = "xml")]
        public string inputXmlPath;
        
        [Sirenix.OdinInspector.FilePath(AbsolutePath = true, Extensions = "csv")]
        public string inputTranslationsPath;
        
        [MenuItem("TG/Social Services/Xbox Achievements Locales")]
        public static void OpenWindow() {
            var window = GetWindow<MicrosoftLocalizedStringsWindow>();
            window.titleContent = new GUIContent("Achievement Localizations Editor");
        }

        [Button]
        void Localize() {
            var localizedTexts = AchievementLocalizationsUtil.ImportLocalizations(inputTranslationsPath);
            
            // Read the XML file
            var xmlContent = System.IO.File.ReadAllText(inputXmlPath);
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlContent);
            
            for (int i = 1; i < xmlDoc.ChildNodes[1].ChildNodes.Count; i++) {
                XmlNode localizedString = xmlDoc.ChildNodes[1].ChildNodes[i];
                string id = localizedString.Attributes![0].Value;
                string text = localizedString.FirstChild.FirstChild.Value;
                
                var localizedText = localizedTexts.FirstOrDefault(t => t.eng == text);
                if (localizedText != null) {
                    localizedString.AppendChild(GetLocalizedText(xmlDoc, "ja-JP", localizedText.ja));
                    localizedString.AppendChild(GetLocalizedText(xmlDoc, "it-IT", localizedText.it));
                    localizedString.AppendChild(GetLocalizedText(xmlDoc, "de-DE", localizedText.de));
                    localizedString.AppendChild(GetLocalizedText(xmlDoc, "fr-FR", localizedText.fr));
                    localizedString.AppendChild(GetLocalizedText(xmlDoc, "zh-CN", localizedText.cns));
                    localizedString.AppendChild(GetLocalizedText(xmlDoc, "zh-TW", localizedText.cnt));
                    localizedString.AppendChild(GetLocalizedText(xmlDoc, "cs-CZ", localizedText.cz));
                    localizedString.AppendChild(GetLocalizedText(xmlDoc, "es-ES", localizedText.es));
                    localizedString.AppendChild(GetLocalizedText(xmlDoc, "pt-BR", localizedText.pt));
                    localizedString.AppendChild(GetLocalizedText(xmlDoc, "ru-RU", localizedText.ru));
                    localizedString.AppendChild(GetLocalizedText(xmlDoc, "pl-PL", localizedText.pl));
                } else {
                    Debug.LogError($"Couldn't find localized text for: {id} - {text}");
                }
            }
            
            // Save the modified XML back to the file
            var outputPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(inputXmlPath)!, "Output.xml");
            xmlDoc.Save(outputPath);
        }

        static XmlElement GetLocalizedText(XmlDocument xmlDoc, string localeCode, string localizedText) {
            XmlElement newValue = xmlDoc.CreateElement("Value", "http://config.mgt.xboxlive.com/schema/localization/1");
            newValue.SetAttribute("locale", localeCode);
            newValue.InnerText = localizedText;
            return newValue;
        }
    }
}