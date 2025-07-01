using Awaken.Utility;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Costs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Items.Weapons {
    public partial class MagicRangedItem : Element<Item> {
        public override ushort TypeForSerialization => SavedModels.MagicRangedItem;

        const int DefaultArrowsRegen = 3;
        
        [Saved] ItemTemplate _magicArrowTemplate;
        [Saved] WeakModelRef<Item> _spawnedMagicArrow;
        [Saved] HeroLoadout[] _loadouts;
        [Saved] List<HeroLoadoutSlotLocker> _lockers = new();
        [Saved] float _magicProjectileManaCost;
        StatCost _spawnCost;
        bool _isSpawning;
        
        ICharacter Owner => ParentModel.Owner.Character;
        ICharacterInventory CharacterInventory => ParentModel.CharacterInventory;
        bool HasArrows => _spawnedMagicArrow.TryGet(out Item spawnedMagicArrow) && spawnedMagicArrow.Quantity > 0;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public MagicRangedItem() {}

        public MagicRangedItem(ItemTemplate magicArrowTemplate, float magicProjectileManaCost, Dictionary<HeroLoadout, EquipmentSlotType> loadouts) {
            _magicArrowTemplate = magicArrowTemplate;
            _magicProjectileManaCost = magicProjectileManaCost;
            _loadouts = loadouts.Keys.ToArray();
        }

        protected override void OnInitialize() {
            _spawnCost = new StatCost(Owner.Stat(CharacterStatType.Mana), _magicProjectileManaCost, skill: ParentModel.CastAbleSkill);
            Owner.ListenTo(Stat.Events.StatChangedBy(CharacterStatType.Mana), OnManaChanged, this);
            ParentModel.ListenTo(Item.Events.Equipped, OnEquip, this);
            Owner.AfterFullyInitialized(InitArrows, this);
        }

        void InitArrows() {
            if (HasArrows) {
                // After restore, arrow might already be spawned, we need to attach the listeners to it
                Item magicArrow = _spawnedMagicArrow.Get();
                magicArrow.ListenTo(Item.Events.QuantityDecreased, _ => SpawnMagicArrow(), magicArrow);
            } else {
                SpawnMagicArrow(false);
            }
        }

        void SpawnMagicArrow(bool payCosts = true) {
            if (HasArrows || _isSpawning) {
                return;
            }
            
            if (payCosts && !_spawnCost.CanAfford()) {
                Hero.Current.Trigger(Hero.Events.StatUseFail, CharacterStatType.Mana);
                Hero.Current.Trigger(Hero.Events.NotEnoughMana, _magicProjectileManaCost);
                return;
            }
            
            _isSpawning = true;
            
            Item magicArrow = CharacterInventory.Add(new Item(_magicArrowTemplate, DefaultArrowsRegen));
            magicArrow.AddElement<LockItemSlot>();
            magicArrow.ListenTo(Item.Events.QuantityDecreased, _ => SpawnMagicArrow(), magicArrow);
            _spawnedMagicArrow = magicArrow;

            if (payCosts) {
                _spawnCost.Pay();
            }
            
            UnlockLoadouts();
            foreach (var loadout in _loadouts) {
                loadout.EquipItem(EquipmentSlotType.Quiver, magicArrow);
            }
            LockLoadouts();
            
            _isSpawning = false;
        }

        // === Helpers
        void UnlockLoadouts() {
            _lockers.ForEach(l => l.Discard());
            _lockers.Clear();
        }
        
        void LockLoadouts() {
            foreach (var loadout in _loadouts) {
                _lockers.Add(loadout.AddElement(new HeroLoadoutSlotLocker(EquipmentSlotType.Quiver)));
            }
        }

        // === Listener Callbacks
        void OnManaChanged(Stat.StatChange statChange) {
            if (statChange.value > 0 && _spawnedMagicArrow.Get() == null && _spawnCost.CanAfford()) {
                SpawnMagicArrow();
            }
        }

        void OnEquip(EquipmentSlotType _) {
            if (_spawnedMagicArrow.Get() == null && _spawnCost.CanAfford()) {
                SpawnMagicArrow();
            }
        }
        
        // === Discard
        protected override void OnDiscard(bool fromDomainDrop) {
            _spawnedMagicArrow.Get()?.Discard();
            UnlockLoadouts();
        }
    }
}