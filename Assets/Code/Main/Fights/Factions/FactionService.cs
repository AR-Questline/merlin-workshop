using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Fights.Factions {
    /// <summary>
    /// Stores gameplay data about factions and their relationships
    /// </summary>
    public partial class FactionService : SerializedService, IDomainBoundService {
        public override ushort TypeForSerialization => SavedServices.FactionService;
        public Domain Domain => Domain.Gameplay;

        FactionTree _tree;
        
        // List of overrides is shared with Faction.
        [Saved] Dictionary<FactionTemplate, List<FactionToFactionAntagonismOverride>> _antagonismOverridesMap = new();
        
        FactionProvider _provider;
        
        public IEnumerable<Faction> AllFactions => _tree.AllFactions;
        
        [UnityEngine.Scripting.Preserve] public Faction Hero => FactionByTemplate(_provider.Hero);
        [UnityEngine.Scripting.Preserve] public Faction Villagers => FactionByTemplate(_provider.Villagers);
        [UnityEngine.Scripting.Preserve] public Faction Bandits => FactionByTemplate(_provider.Bandits);
        [UnityEngine.Scripting.Preserve] public Faction DalRiata => FactionByTemplate(_provider.DalRiata);
        [UnityEngine.Scripting.Preserve] public Faction Picts => FactionByTemplate(_provider.Picts);
        [UnityEngine.Scripting.Preserve] public Faction Kamelot => FactionByTemplate(_provider.Kamelot);
        [UnityEngine.Scripting.Preserve] public Faction Monsters => FactionByTemplate(_provider.Monsters);
        [UnityEngine.Scripting.Preserve] public Faction PreyAnimals => FactionByTemplate(_provider.PreyAnimals);
        [UnityEngine.Scripting.Preserve] public Faction HunterAnimals => FactionByTemplate(_provider.HunterAnimals);
        [UnityEngine.Scripting.Preserve] public Faction DomesticAnimals => FactionByTemplate(_provider.DomesticAnimals);
        [UnityEngine.Scripting.Preserve] public Faction LargeHunterAnimals => FactionByTemplate(_provider.LargeHunterAnimals);
        [UnityEngine.Scripting.Preserve] public Faction SmallPreyAnimals => FactionByTemplate(_provider.SmallPreyAnimals);
        public Faction Humans => FactionByTemplate(_provider.Humans);
        
        public static class Events {
            public static readonly Event<ICharacter, ICharacter> AntagonismChanged = new(nameof(AntagonismChanged));
        }
        
        public void Init() {
            _provider = World.Services.Get<FactionProvider>();
            FactionTemplate[] factions = World.Services.Get<TemplatesProvider>().GetAllOfType<FactionTemplate>().ToArray();
            _tree = new FactionTree(factions);
        }
        
        public void AddAntagonismOverride(FactionTemplate factionTemplate, FactionToFactionAntagonismOverride antagonismOverride) {
            Faction faction = FactionByTemplate(factionTemplate);
            if (!_antagonismOverridesMap.TryGetValue(factionTemplate, out var list)) {
                list = new List<FactionToFactionAntagonismOverride>();
                faction.SetAntagonismOverridesList(list);
                _antagonismOverridesMap.Add(factionTemplate, list);
            }
            list.RemoveAll(o => o.TargetFactionTemplate == antagonismOverride.TargetFactionTemplate);
            if (antagonismOverride.Antagonism != faction.DefaultAntagonismTo(FactionByTemplate(antagonismOverride.TargetFactionTemplate))) {
                list.Add(antagonismOverride);
            } else if (list.IsEmpty()) {
                _antagonismOverridesMap.Remove(factionTemplate);
                faction.RemoveAntagonismOverridesList();
            }
        }

        void UnapplyAllAppliedAntagonismOverrides() {
            foreach (var faction in AllFactions) {
                faction.RemoveAntagonismOverridesList();
            }
        }
        
        void ApplySavedAntagonismOverrides() {
            foreach (var keyValuePair in _antagonismOverridesMap) {
                FactionByTemplate(keyValuePair.Key).SetAntagonismOverridesList(keyValuePair.Value);
            }
        }

        public Faction FactionByTemplate(FactionTemplate template) => _tree.FactionByTemplate(template);

        public bool RemoveOnDomainChange() {
            return true;
        }

        public override void OnAfterDeserialize() {
            UnapplyAllAppliedAntagonismOverrides();
            ApplySavedAntagonismOverrides();
        }
    }
}