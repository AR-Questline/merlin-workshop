using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Weapons {
    public partial class ItemStatsRequirements : Element<Item> {
        public override ushort TypeForSerialization => SavedModels.ItemStatsRequirements;

        [Saved] ItemRequirementsWrapper _wrapper;
        ItemStatsRequirementsAttachment _dataSource;
        
        public Stat StrengthRequired { get; private set; }
        public Stat DexterityRequired { get; private set; }
        public Stat SpiritualityRequired { get; private set; }
        
        public Stat PerceptionRequired { get; private set; }
        public Stat EnduranceRequired { get; private set; }
        public Stat PracticalityRequired { get; private set; }

        bool _cacheUpdateRequired = true;
        int _missingPointsCache = -1;
        
        public int MissingRequirementPoints => _cacheUpdateRequired ? _missingPointsCache = CalculateMissingPoints() : _missingPointsCache;
        public bool RequirementsMet => MissingRequirementPoints <= 0;
        
        // === Constructors
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public ItemStatsRequirements() { }
        
        // === Initialization
        protected override void OnInitialize() {
            _dataSource = ParentModel.Template.GetAttachment<ItemStatsRequirementsAttachment>();
            _wrapper.Initialize(this);

            Hero.Current.ListenTo(StatType.Events.StatOfTypeChanged<HeroRPGStatType>(), CacheUpdateRequired, this);
            ParentModel.ListenTo(StatType.Events.StatOfTypeChanged<ItemRequirementStatType>(), CacheUpdateRequired, this);
        }

        int CalculateMissingPoints() {
            HeroRPGStats heroRPGStats = Hero.Current.HeroRPGStats;
            _cacheUpdateRequired = false;

            return Mathf.CeilToInt(
                math.max(0, StrengthRequired.ModifiedValue - heroRPGStats.Strength.ModifiedValue) +
                math.max(0, DexterityRequired.ModifiedValue - heroRPGStats.Dexterity.ModifiedValue) +
                math.max(0, SpiritualityRequired.ModifiedValue - heroRPGStats.Spirituality.ModifiedValue) +
                math.max(0, PerceptionRequired.ModifiedValue - heroRPGStats.Perception.ModifiedValue) +
                math.max(0, EnduranceRequired.ModifiedValue - heroRPGStats.Endurance.ModifiedValue) +
                math.max(0, PracticalityRequired.ModifiedValue - heroRPGStats.Practicality.ModifiedValue));
        }
        
        void CacheUpdateRequired(Stat _) {
            _cacheUpdateRequired = true;
            ParentModel.TryGetElement<ItemSkillsInvoker>()?.RequirementsChanged(RequirementsMet);
        }
        
        // === Persistence
        
        void OnBeforeWorldSerialize() {
            _wrapper.PrepareForSave(this);
        }
        
        public partial struct ItemRequirementsWrapper {
            public ushort TypeForSerialization => SavedTypes.ItemRequirementsWrapper;

            [Saved(0f)] float StrengthRequiredDif;
            [Saved(0f)] float DexterityRequiredDif;
            [Saved(0f)] float SpiritualityRequiredDif;
            [Saved(0f)] float PerceptionRequiredDif;
            [Saved(0f)] float EnduranceRequiredDif;
            [Saved(0f)] float PracticalityRequiredDif;
            
            public void Initialize(ItemStatsRequirements statsRequirements) {
                Item parentModel = statsRequirements.ParentModel;
                ItemStatsRequirementsAttachment dataSource = statsRequirements._dataSource;
                
                statsRequirements.StrengthRequired = new Stat(parentModel, ItemRequirementStatType.StrengthRequired, dataSource.strengthRequired + StrengthRequiredDif);
                statsRequirements.DexterityRequired = new Stat(parentModel, ItemRequirementStatType.DexterityRequired, dataSource.dexterityRequired + DexterityRequiredDif);
                statsRequirements.SpiritualityRequired = new Stat(parentModel, ItemRequirementStatType.SpiritualityRequired, dataSource.spiritualityRequired + SpiritualityRequiredDif);
                
                statsRequirements.PerceptionRequired = new Stat(parentModel, ItemRequirementStatType.PerceptionRequired, dataSource.perceptionRequired + PerceptionRequiredDif);
                statsRequirements.EnduranceRequired = new Stat(parentModel, ItemRequirementStatType.EnduranceRequired, dataSource.enduranceRequired + EnduranceRequiredDif);
                statsRequirements.PracticalityRequired = new Stat(parentModel, ItemRequirementStatType.PracticalityRequired, dataSource.practicalityRequired + PracticalityRequiredDif);
            }

            public void PrepareForSave(ItemStatsRequirements itemStatsStats) {
                ItemStatsRequirementsAttachment dataSource = itemStatsStats._dataSource;

                StrengthRequiredDif = itemStatsStats.StrengthRequired.BaseValue - dataSource.strengthRequired;
                DexterityRequiredDif = itemStatsStats.DexterityRequired.BaseValue - dataSource.dexterityRequired;
                SpiritualityRequiredDif = itemStatsStats.SpiritualityRequired.BaseValue - dataSource.spiritualityRequired;
                
                PerceptionRequiredDif = itemStatsStats.PerceptionRequired.BaseValue - dataSource.perceptionRequired;
                EnduranceRequiredDif = itemStatsStats.EnduranceRequired.BaseValue - dataSource.enduranceRequired;
                PracticalityRequiredDif = itemStatsStats.PracticalityRequired.BaseValue - dataSource.practicalityRequired;
            }
        }
    }
}