using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Locations.Spawners.Arena {
    public class ArenaSpawnerData {
        static string s_prefsKeyRecentChoices = $"{nameof(ArenaSpawnerData)};{nameof(s_prefsKeyRecentChoices)}";
        static string s_prefsKeyFavourites = $"{nameof(ArenaSpawnerData)};{nameof(s_prefsKeyFavourites)}";

        const int MaxRecentChoices = 9;

        static IEnumerable<LocationTemplate> LocationTemplates => World.Services.Get<TemplatesProvider>()
            .GetAllOfType<LocationTemplate>(TemplateTypeFlag.All)
            .Where(t => t.gameObject.GetComponent<NpcAttachment>() != null);

        static IEnumerable<LocationTemplate> NewNpcsLocationTemplates => LocationTemplates.Where(t => t.gameObject.GetComponent<NpcAttachment>().IsNew);
        static IEnumerable<EncounterData> EncountersData => EncountersCache.Get.encounters.Select(p => p.data).SelectMany(p => p);

        public List<IArenaSpawnerEntry> AllData { get; } = new();
        public List<IArenaSpawnerEntry> News { get; private set; } = new();
        public List<IArenaSpawnerEntry> Encounters { get; private set; } = new();
        public List<IArenaSpawnerEntry> Other { get; private set; } = new();
        public List<IArenaSpawnerEntry> Favourites { get; } = new();
        public List<IArenaSpawnerEntry> RecentChoices { get; } = new();

        public ArenaSpawnerData() {
            InitData();
            Load();
        }

        public void AddOrRemoveFavourite(IArenaSpawnerEntry entry) {
            if (Favourites.Contains(entry)) {
                Favourites.Remove(entry);
            } else {
                Favourites.Add(entry);
            }

            Save(Favourites, s_prefsKeyFavourites);
        }

        public void AddRecentlyChose(IArenaSpawnerEntry entry) {
            if (RecentChoices.Contains(entry)) {
                RecentChoices.Remove(entry);
            } else if (RecentChoices.Count >= MaxRecentChoices) {
                var lastElement = RecentChoices[^1];
                RecentChoices.Remove(lastElement);
            }

            RecentChoices.Insert(0, entry);
            Save(RecentChoices, s_prefsKeyRecentChoices);
        }

        void InitData() {
            News = NewNpcsLocationTemplates.Select(p => new LocationEntry(p) as IArenaSpawnerEntry).ToList();
            Encounters = EncountersData.Select(p => new EncounterEntry(p) as IArenaSpawnerEntry).ToList();
            Other = LocationTemplates.Select(p => new LocationEntry(p) as IArenaSpawnerEntry).ToList();
            AllData.AddRange(Other.Concat(Encounters));
        }

        void Load() {
            LoadData(RecentChoices, s_prefsKeyRecentChoices);
            LoadData(Favourites, s_prefsKeyFavourites);
            return;

            void LoadData(List<IArenaSpawnerEntry> list, string generalKey) {
                int i = 0;
                string directKey = GetDirectKey(generalKey, i);
                while (PrefMemory.HasKey(directKey)) {
                    var keyValue = PrefMemory.GetString(directKey);
                    var entry = AllData.Find(a => a.PersistentId == keyValue);
                    list.Add(entry);
                    directKey = GetDirectKey(generalKey, ++i);
                }
            }
        }

        void Save(List<IArenaSpawnerEntry> entries, string generalKey) {
            int i = 0;
            string directKey = GetDirectKey(generalKey, i);
            
            // save current values
            while (i < entries.Count) {
                PrefMemory.Set(directKey, entries[i].PersistentId, false);
                directKey = GetDirectKey(generalKey, ++i);
            }
            
            // delete excess keys
            while (PrefMemory.HasKey(directKey)) {
                PrefMemory.DeleteKey(directKey);
                directKey = GetDirectKey(generalKey, ++i);
            }
            PrefMemory.Save();
        }

        string GetDirectKey(string generalKey, int i) => $"{generalKey};{i})";
    }
}