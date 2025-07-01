using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs;
using Awaken.TG.Main.Memories.Journal.Conditions;
using Awaken.TG.Main.Memories.Journal.Entries;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.Journal;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Unity.IL2CPP.CompilerServices;

namespace Awaken.TG.Main.Memories.Journal {
    public partial class PlayerJournal : Model {
        public override ushort TypeForSerialization => SavedModels.PlayerJournal;

        public override Domain DefaultDomain => Domain.Gameplay;
        
        [Saved] HashSet<SerializableGuid> _unlockedEntries = new(100);
        
        // entry cache by type
        readonly Dictionary<Type, List<EntryData>> _entryCache = new();
        readonly Dictionary<Guid, EntryData> _conditionalEntryCache = new();
        bool _treatAllEntriesAsUnlocked;
        
        [UnityEngine.Scripting.Preserve]
        public IReadOnlyCollection<SerializableGuid> UnlockedEntries => _unlockedEntries;
        public IEnumerable<T> GetEntries<T>() where T : EntryData => _entryCache.TryGetValue(typeof(T), out List<EntryData> list) ? list.Cast<T>() : new List<T>();

        [Il2CppEagerStaticClassConstruction]
        public new static class Events {
            public static readonly Event<PlayerJournal, Guid> EntryUnlocked = new(nameof(EntryUnlocked));
        }

        protected override void OnInitialize() {
            FillCache();
            InitializeEntries();
        }

        void FillCache() {
            foreach (EntryData data in CommonReferences.Get.Journal.GetEntryDatas()) {
                FillEntryCache(data);
                FillConditionalCache(data);
            }
        }
        
        void FillEntryCache(EntryData data) {
            if (!_entryCache.TryGetValue(data.GetType(), out List<EntryData> list)) {
                list = new List<EntryData>();
                _entryCache.Add(data.GetType(), list);
            }
            
            list.Add(data);
        }
        
        void FillConditionalCache(EntryData data) {
            AddConditionToCache(data.conditionForEntry, data);

            data.GetEntries().ForEach(subEntryData => {
                AddConditionToCache(subEntryData.Condition, data);
            });
            
            return;

            void AddConditionToCache(ConditionData conditionData, EntryData entryData) {
                if (conditionData is Condition condition) {
                    if (condition.Guid.GUID.Guid.Equals(default)) {
                        Log.Important?.ErrorThenLogs($"[Once] Condition {condition.GetType().Name} {entryData.EntryName} has empty GUID", Log.Utils.PlayerJournal);
                        return;
                    }
                    if (!_conditionalEntryCache.TryAdd(condition.Guid.GUID, data)) {
                        Log.Important?.ErrorThenLogs($"[Once] Duplicate {condition.GetType().Name} {condition.Guid.GUID} in journal entry {entryData.EntryName}", Log.Utils.PlayerJournal);
                    }
                }
            }
        }
        
        void InitializeEntries() {
            foreach (EntryData data in _entryCache.Values.SelectMany(e => e)) {
                if (data.InitializedSeparately) continue;
                data.GenericInitialize(this);
            }        
            // Any added models by entries as children of this will get their OnInitialize here
        }

        // === Entry API ===
        public void UnlockEntry(Guid entryGuid, JournalSubTabType journalTabType) {
            if (entryGuid == SerializableGuid.Empty) {
                return;
            }
            
            if (!_unlockedEntries.Add(new(entryGuid))) {
                return;
            }
            
            World.EventSystem.Trigger(this, Events.EntryUnlocked, entryGuid);
            var entryData = _conditionalEntryCache.GetValueOrDefault(entryGuid);
            string name = entryData?.EntryName ?? string.Empty;
            SendNotification(name, journalTabType);
        }

        public bool WasEntryUnlocked(Guid entryGuid) => _treatAllEntriesAsUnlocked || _unlockedEntries.Contains(new(entryGuid));

        public void TreatAllEntriesAsUnlocked(bool unlocked = true) {
            _treatAllEntriesAsUnlocked = unlocked;
        }

        public void SendNotification(string name, JournalSubTabType journalTabType) {
            if (string.IsNullOrEmpty(name)) {
                return;
            }
            
            AdvancedNotificationBuffer.Push<JournalUnlockNotificationBuffer>(new JournalUnlockNotification(name, journalTabType));
        }
    }
}
