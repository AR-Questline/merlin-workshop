using System.Collections.Generic;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Memories.Journal.Entries {
    public abstract class EntryRuntimeBase {
        public bool IsInitialized { get; private set; }
        protected EntryData Data { get; private set; }

        public void Initialize(Model owner, EntryData data) {
            if (IsInitialized) throw new System.Exception("Already initialized");
            Data = data;
            OnInitialize(owner);
            IsInitialized = true;
        }
        
        [UnityEngine.Scripting.Preserve] public IEnumerable<SubEntryData> GetEntries() => Data.GetEntries();
        
        protected abstract void OnInitialize(Model owner);
    }
}
