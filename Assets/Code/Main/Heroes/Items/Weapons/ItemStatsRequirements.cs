using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Containers;
using Awaken.TG.Main.NewGamePlus;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Relations;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Weapons {
    public partial class ItemStatsRequirements : Element<Item>, IItemOwnerRelatedElement {
        public override ushort TypeForSerialization => SavedModels.ItemStatsRequirements;

        [Saved] ItemRequirementsWrapper _wrapper;
        ItemStatsRequirementsAttachment _dataSource;
        
        public Stat StrengthRequired { get; private set; }
        public Stat DexterityRequired { get; private set; }
        public Stat SpiritualityRequired { get; private set; }
        
        public Stat PerceptionRequired { get; private set; }
        public Stat EnduranceRequired { get; private set; }
        public Stat PracticalityRequired { get; private set; }

        bool _cacheUpdateRequired = false;
        int _missingPointsCache = -1;
        IEventListener _heroStatChangeListener, _itemRequirementStatChangeListener;
        
        public int MissingRequirementPoints => _cacheUpdateRequired ? _missingPointsCache = CalculateMissingPoints() : _missingPointsCache;
        public bool RequirementsMet => MissingRequirementPoints <= 0;
        
        // === Constructors
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public ItemStatsRequirements() { }
        
        // === Initialization
        protected override void OnInitialize() {
            _dataSource = ParentModel.Template.GetAttachment<ItemStatsRequirementsAttachment>();
        }

        protected override void OnRestore() {
            _dataSource = ParentModel.Template.GetAttachment<ItemStatsRequirementsAttachment>();
            TryInit(ParentModel.Owner);
        }

        public void AfterOwnerAdded(RelationEventData data) {
            TryInit((IItemOwner) data.to);
        }

        public void BeforeOwnerRemoved(RelationEventData data) {
            Cleanup();
        }

        void TryInit(IItemOwner owner) {
            if (owner is Location or ContainerInventory) {
                Cleanup();
            } else {
                // Only Hero and storages showing full item info (HeroStorage and Shops) should have stats requirements
                Init();
            }
        }
        
        void Init() {
            _cacheUpdateRequired = true;
            _wrapper.Initialize(this);
            InitListeners();
        }
        
        void InitListeners() {
            _heroStatChangeListener = Hero.Current.ListenTo(StatType.Events.StatOfTypeChanged<HeroRPGStatType>(), CacheUpdateRequired, this);
            _itemRequirementStatChangeListener = ParentModel.ListenTo(StatType.Events.StatOfTypeChanged<ItemRequirementStatType>(), CacheUpdateRequired, this);
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

        void Cleanup() {
            _cacheUpdateRequired = false;
            _missingPointsCache = -1;
            _wrapper.PrepareForSave(this);
            World.EventSystem.TryDisposeListener(ref _heroStatChangeListener);
            World.EventSystem.TryDisposeListener(ref _itemRequirementStatChangeListener);
            
            StrengthRequired = null;
            DexterityRequired = null;
            SpiritualityRequired = null;
            PerceptionRequired = null;
            EnduranceRequired = null;
            PracticalityRequired = null;
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
                NewGamePlusSystem.GetAllStatsRequirements(parentModel.NewGamePlusLevel, dataSource, 
                    out int str, out int dex, out int spi, out int per, out int end, out int pra);
                
                statsRequirements.StrengthRequired = new Stat(parentModel, ItemRequirementStatType.StrengthRequired, str + StrengthRequiredDif);
                statsRequirements.DexterityRequired = new Stat(parentModel, ItemRequirementStatType.DexterityRequired, dex + DexterityRequiredDif);
                statsRequirements.SpiritualityRequired = new Stat(parentModel, ItemRequirementStatType.SpiritualityRequired, spi + SpiritualityRequiredDif);
                
                statsRequirements.PerceptionRequired = new Stat(parentModel, ItemRequirementStatType.PerceptionRequired, per + PerceptionRequiredDif);
                statsRequirements.EnduranceRequired = new Stat(parentModel, ItemRequirementStatType.EnduranceRequired, end + EnduranceRequiredDif);
                statsRequirements.PracticalityRequired = new Stat(parentModel, ItemRequirementStatType.PracticalityRequired, pra + PracticalityRequiredDif);
            }

            public void PrepareForSave(ItemStatsRequirements itemStatsStats) {
                ItemStatsRequirementsAttachment dataSource = itemStatsStats._dataSource;
                if (itemStatsStats.StrengthRequired == null) {
                    return;
                }
                NewGamePlusSystem.GetAllStatsRequirements(itemStatsStats.ParentModel.NewGamePlusLevel, dataSource, 
                    out int str, out int dex, out int spi, out int per, out int end, out int pra);
                
                StrengthRequiredDif = itemStatsStats.StrengthRequired.ValueForSave - str;
                DexterityRequiredDif = itemStatsStats.DexterityRequired.ValueForSave - dex;
                SpiritualityRequiredDif = itemStatsStats.SpiritualityRequired.ValueForSave - spi;
                
                PerceptionRequiredDif = itemStatsStats.PerceptionRequired.ValueForSave - per;
                EnduranceRequiredDif = itemStatsStats.EnduranceRequired.ValueForSave - end;
                PracticalityRequiredDif = itemStatsStats.PracticalityRequired.ValueForSave - pra;
            }
        }
    }
}