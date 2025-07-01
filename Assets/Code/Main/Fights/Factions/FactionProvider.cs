using System;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Factions {
    /// <summary>
    /// Provides immutable data about factions
    /// </summary>
    public class FactionProvider : MonoBehaviour, IService {

        bool _initialized;
        
        [SerializeField, TableList(IsReadOnly = true, CellPadding = 5, ShowIndexLabels = true)]
        ReputationRow[] table = new ReputationRow[4];
        
        [SerializeField, TemplateType(typeof(FactionTemplate))] TemplateReference _rootReference;
        [SerializeField, TemplateType(typeof(FactionTemplate))] TemplateReference _heroReference;
        [SerializeField, TemplateType(typeof(FactionTemplate))] TemplateReference _villagersReference;
        [SerializeField, TemplateType(typeof(FactionTemplate))] TemplateReference _banditsReference;
        [SerializeField, TemplateType(typeof(FactionTemplate))] TemplateReference _dalRiataReference;
        [SerializeField, TemplateType(typeof(FactionTemplate))] TemplateReference _pictsReference;
        [SerializeField, TemplateType(typeof(FactionTemplate))] TemplateReference _kamelotReference;
        [SerializeField, TemplateType(typeof(FactionTemplate))] TemplateReference _monstersReference;
        [SerializeField, TemplateType(typeof(FactionTemplate))] TemplateReference _preyAnimalsReference;
        [SerializeField, TemplateType(typeof(FactionTemplate))] TemplateReference _hunterAnimalsReference;
        [SerializeField, TemplateType(typeof(FactionTemplate))] TemplateReference _domesticAnimalsReference;
        [SerializeField, TemplateType(typeof(FactionTemplate))] TemplateReference _largeHunterAnimalsReference;
        [SerializeField, TemplateType(typeof(FactionTemplate))] TemplateReference _smallPreyAnimalsReference;
        [SerializeField, TemplateType(typeof(FactionTemplate))] TemplateReference _humansReference;

        public FactionTemplate Root { get; private set; }
        public FactionTemplate Hero { get; private set; }
        public FactionTemplate Villagers { get; private set; }
        public FactionTemplate Bandits { get; private set; }
        public FactionTemplate DalRiata { get; private set; }
        public FactionTemplate Picts { get; private set; }
        public FactionTemplate Kamelot { get; private set; }
        public FactionTemplate Monsters { get; private set; }
        public FactionTemplate PreyAnimals { get; private set; }
        public FactionTemplate HunterAnimals { get; private set; }
        public FactionTemplate DomesticAnimals { get; private set; }
        public FactionTemplate LargeHunterAnimals { get; private set; }
        public FactionTemplate SmallPreyAnimals { get; private set; }
        public FactionTemplate Humans { get; private set; }
        
        public ReputationMatrix ReputationMatrix { get; private set; }
        
        public void EnsureInitialized() {
            if (_initialized) {
                return;
            }

            ReputationMatrix = new ReputationMatrix(table);
            
            Root = _rootReference.Get<FactionTemplate>();
            Hero = _heroReference.Get<FactionTemplate>();
            Villagers = _villagersReference.Get<FactionTemplate>();
            Bandits = _banditsReference.Get<FactionTemplate>();
            DalRiata = _dalRiataReference.Get<FactionTemplate>();
            Picts = _pictsReference.Get<FactionTemplate>();
            Kamelot = _kamelotReference.Get<FactionTemplate>();
            Monsters = _monstersReference.Get<FactionTemplate>();
            PreyAnimals = _preyAnimalsReference.Get<FactionTemplate>();
            HunterAnimals = _hunterAnimalsReference.Get<FactionTemplate>();
            DomesticAnimals = _domesticAnimalsReference.Get<FactionTemplate>();
            LargeHunterAnimals = _largeHunterAnimalsReference.Get<FactionTemplate>();
            SmallPreyAnimals = _smallPreyAnimalsReference.Get<FactionTemplate>();
            Humans = _humansReference.Get<FactionTemplate>();

            _initialized = true;
        }
    }
}