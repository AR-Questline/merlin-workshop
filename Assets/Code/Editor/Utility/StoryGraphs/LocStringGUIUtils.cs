using System.Collections.Generic;
using System.Reflection;
using Awaken.TG.Editor.Localizations;
using Awaken.TG.Main.Localization;
using Awaken.Utility;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Awaken.TG.Editor.Utility.StoryGraphs {
    public static class LocStringGUIUtils {
        static readonly Dictionary<string, LocStringData> LocStringCache = new();
        
        public static void ClearLocalizationCache() {
            LocStringCache.Clear();
        }
        
        public static LocStringData GetData(SerializedProperty serializedProperty, FieldInfo field, int nodeWidth) {
            LocStringData data = new();

            // get text field
            LocString text = (LocString) serializedProperty.GetPropertyValue();
            data.id = text.ID;
            if (LocStringCache.TryGetValue(data.id, out LocStringData valueData)) {
                return valueData;
            }
            
            data.textString = LocalizationHelper.Translate(text.ID, LocalizationHelper.SelectedLocale, true);
            
            // get correct string table
            data.stringCollection = LocalizationUtils.DetermineStringTable(serializedProperty, false);
            data.stringTable = data.stringCollection?.GetTable(LocalizationSettings.ProjectLocale.Identifier) as StringTable;
            
            // validate term ID
            string localizationPrefix = NodeGUIUtil.Graph(serializedProperty).LocalizationPrefix;
            data.wasChanged = LocalizationUtils.ValidateTerm(serializedProperty, localizationPrefix, out string newTerm, field);
            if (data.wasChanged) {
                text.ID = newTerm;
                data.id = text.ID;
            }
            
            // setup text area
            data.height = NodeGUIUtil.SetupTextArea(field, nodeWidth, data.textString);

            LocStringCache[data.id] = data;
            return data;
        }
    }
    
    public struct LocStringData {
        public string id;
        public string textString;
        public StringTableCollection stringCollection;
        public StringTable stringTable;
        public bool wasChanged;
        public float height;
    }
}