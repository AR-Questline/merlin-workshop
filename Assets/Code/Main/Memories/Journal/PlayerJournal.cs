using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs;
using Awaken.TG.Main.Memories.Journal.Conditions;
using Awaken.TG.Main.Memories.Journal.Entries;
using Awaken.TG.Main.Memories.Journal.Entries.Implementations;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Steps.Helpers;
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
        
        // Track last unlocked entry for recent journal opening
        LastUnlockedEntryInfo _lastUnlockedEntry;
        
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
            World.Any<JournalUnlockNotificationBuffer>()?.ClearBuffer();
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
            foreach (var conditionData in data.GetAllConditions()) {
                if (conditionData is Condition condition) {
                    if (condition.Guid.GUID.Guid.Equals(default)) {
                        Log.Important?.ErrorThenLogs($"[Once] Condition {condition.GetType().Name} {data.EntryName} has empty GUID", Log.Utils.PlayerJournal);
                        return;
                    }
                    if (!_conditionalEntryCache.TryAdd(condition.Guid.GUID, data)) {
                        Log.Important?.ErrorThenLogs($"[Once] Duplicate {condition.GetType().Name} {condition.Guid.GUID} in journal entry {data.EntryName}", Log.Utils.PlayerJournal);
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
        public void UnlockEntry(Guid entryGuid, JournalSubTabType journalTabType = null) {
            if (entryGuid == SerializableGuid.Empty) {
                return;
            }
            
            if (!_unlockedEntries.Add(new(entryGuid))) {
                return;
            }
            
            World.EventSystem.Trigger(this, Events.EntryUnlocked, entryGuid);
            var entryData = _conditionalEntryCache.GetValueOrDefault(entryGuid);
            if (entryData == null) {
                Log.Important?.ErrorThenLogs($"No entry found for condition GUID {entryGuid}", Log.Utils.PlayerJournal);
                return;
            }

            if (!entryData.conditionForEntry.Validate(entryData.conditionForEntry is Condition condition && entryGuid != condition.Guid.GUID)) {
                return;
            }
            
            // Skip notification for subentries when main entry is not unlocked
            if (ShouldSkipNotification(entryGuid, entryData)) {
                return;
            }
            
            string name = entryData.EntryName ?? string.Empty;
            journalTabType ??= GetJournalSubTabType(entryData);
            UnlockEntry(name, journalTabType);
        }
        
        public void UnlockEntry(string entryName, JournalSubTabType journalTabType) {
            // Track last unlocked entry for recent journal opening
            _lastUnlockedEntry = new LastUnlockedEntryInfo(entryName, journalTabType);
            SendNotification(entryName, journalTabType);
        }

        public bool WasEntryUnlocked(Guid entryGuid) => _treatAllEntriesAsUnlocked || _unlockedEntries.Contains(new(entryGuid));

        public void TreatAllEntriesAsUnlocked(bool unlocked = true) {
            _treatAllEntriesAsUnlocked = unlocked;
        }

        public LastUnlockedEntryInfo GetLastUnlockedEntry() {
            return _lastUnlockedEntry.IsValid()
                ? _lastUnlockedEntry
                : default;
        }
        
        public void ClearLastUnlockedEntry() {
            _lastUnlockedEntry = default;
        }

        public void SendNotification(string name, JournalSubTabType journalTabType) {
            if (string.IsNullOrEmpty(name)) {
                return;
            }
            
            NotificationUtils.Push(new JournalUnlockNotification(name, journalTabType));
        }
        
        bool ShouldSkipNotification(Guid entryGuid, EntryData entryData) {
            if (entryData.conditionForEntry is Condition mainEntryCondition) {
                if (mainEntryCondition.Guid.GUID.Equals(entryGuid)) {
                    return false;
                }
                
                return !WasEntryUnlocked(mainEntryCondition.Guid.GUID);
            }
            
            return false;
        }

        JournalSubTabType GetJournalSubTabType(EntryData entryData) {
            switch (entryData) {
                case BeastiaryRuntime.BeastiaryData:
                    return JournalSubTabType.Bestiary;
                case CharacterRuntime.CharacterData:
                    return JournalSubTabType.Characters;
                case LoreEntryRuntime.LoreJournalData:
                    return JournalSubTabType.Lore;
                default:
                    Log.Important?.Error($"Unknown journal entry type for data: {entryData}");
                    return JournalSubTabType.Bestiary;
            }
        }
    }
    
    public readonly struct LastUnlockedEntryInfo {
        public readonly string entryName;
        public readonly JournalSubTabType tabType;
        readonly float _lastUnlockTime;
        
        public LastUnlockedEntryInfo(string entryName, JournalSubTabType tabType) {
            this.entryName = entryName;
            this.tabType = tabType;
            _lastUnlockTime = UnityEngine.Time.realtimeSinceStartup;
        }

        public bool IsValid() {
            return UnityEngine.Time.realtimeSinceStartup - _lastUnlockTime <= GameConstants.Get.journalLastEntryAvailabilityTime &&
                   !string.IsNullOrEmpty(entryName) &&
                   tabType != null;
        }
    }
}
