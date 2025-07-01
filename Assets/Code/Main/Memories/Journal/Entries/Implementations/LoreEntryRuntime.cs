using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Memories.Journal.Conditions;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Memories.Journal.Entries.Implementations {
    [Serializable]
    public class LoreEntryRuntime : EntryRuntimeBase {
        protected override void OnInitialize(Model owner) {
            foreach (SubEntryData conditionedSubentry in Data.GetEntries()) {
                if (conditionedSubentry == null) {
                    Log.Debug?.Error("Conditioned subentry is null in LoreEntry: " + Data.EntryName);
                    continue;
                }
                ConditionData condition = conditionedSubentry.Condition;
                if (condition == null) {
                    Log.Debug?.Error($"Condition data is null for subentry: {conditionedSubentry.TextToShow.Translate()}");
                    continue;
                }
                if (condition.IsMet()) {
                    Log.Debug?.Info($"Condition met: {conditionedSubentry.ElementLabelText()} \n{conditionedSubentry.TextToShow.Translate()}");
                } else {
                    condition.Initialize(owner);
                }
            }
        }
        
        [Serializable]
        public class LoreJournalData : EntryData<LoreEntryRuntime> {
            [SerializeField, LocStringCategory(Category.Journal)] [UnityEngine.Scripting.Preserve] public LocString name;
            [SerializeField, LocStringCategory(Category.Journal)] public LocString description;
            
            [UIAssetReference] 
            public ShareableSpriteReference image;
            
            [Space(20)]
            [SerializeField, ListDrawerSettings(ListElementLabelName = nameof(SubEntryData.ElementLabelText))] 
            SubEntryData[] subentries;
            
            public override IEnumerable<SubEntryData> GetEntries() => subentries;
        }
    }
}
