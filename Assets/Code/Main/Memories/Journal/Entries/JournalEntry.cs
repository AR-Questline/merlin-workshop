using System.Linq;
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Memories.Journal.Entries {
    [AddComponentMenu("Journal/Entry")]
    public class JournalEntry : JournalEntryTemplateData {
        [Title("@" + nameof(DataTitle))]
        [SerializeReference, InlineProperty, HideLabel]
        EntryData data;
        
        public override EntryData GenericData => data;
        
        // === Odin
        string DataTitle => data == null ? "Select entry type" : data.GetType().Name.Replace("Data", " Entry");

#if UNITY_EDITOR
        [Button, PropertyOrder(-1), HorizontalGroup("TopButtons")]
        void SetNameFromEntryName() {
            gameObject.name = RemoveDiacritics(data.EntryName);
        }
        
        static string RemoveDiacritics(string str) {
            if (null == str) return null;
            var chars = str
                        .Normalize(NormalizationForm.FormD)
                        .ToCharArray()
                        .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                        .ToArray();

            return new string(chars).Normalize(NormalizationForm.FormC);
        }
#endif
    }
}
