using System;
using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Memories.Journal.Conditions;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Memories.Journal.Entries {
    [Serializable]
    public abstract class EntryData {
        [SerializeField, LocStringCategory(Category.Journal)] 
        protected LocString entryName;
        [SerializeReference, LabelText("@" + nameof(ConditionText) + "()")] 
        public ConditionData conditionForEntry;
        
        public string EntryName => entryName.Translate();
        
        public virtual bool InitializedSeparately => false;
        public abstract EntryRuntimeBase GenericInitialize(Model owner);

        public abstract IEnumerable<SubEntryData> GetEntries();
        
        string ConditionText() {
            return ConditionData.ConditionInfo(conditionForEntry);
        }
        
#if UNITY_EDITOR
        public LocString EDITOR_EntryName => entryName;
        public ConditionData EDITOR_Condition => conditionForEntry;
#endif
    }
    
    [Serializable]
    public abstract class EntryData<TRuntime> : EntryData
        where TRuntime : EntryRuntimeBase, new() {
        
        public TRuntime Initialize(Model owner) {
            TRuntime instance = new();
            instance.Initialize(owner, this);
            conditionForEntry.Initialize(owner);
            return instance;
        }

        public sealed override EntryRuntimeBase GenericInitialize(Model owner) => Initialize(owner);
    }
}
