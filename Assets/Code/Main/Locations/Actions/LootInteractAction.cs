using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;
using Sirenix.Utilities;

namespace Awaken.TG.Main.Locations.Actions {
    /// <summary>
    /// Loot Interact is used for locations that are supposed to get hit to drop loot (mining)
    /// </summary>
    public partial class LootInteractAction : ToolInteractAction, IRefreshedByAttachment<LootInteractAttachment> {
        public override ushort TypeForSerialization => SavedModels.LootInteractAction;

        const int DefaultHealth = 3;

        bool _overrideHealth;
        ILootTable _lootTable;

        public IEnumerable<ItemSpawningDataRuntime> Loot =>
            ItemUtils.GetItemSpawningDataFromLootTable(_lootTable, ParentModel.Spec, this).Where(x => x?.ItemTemplate != null);

        public override bool IsIllegal {
            get {
                if (Loot.TryGetFirst(out var data)) {
                    using var crime = Crime.Theft(data, ParentModel);
                    return crime.IsCrime();
                }

                return false;
            }
        }

        public void InitFromAttachment(LootInteractAttachment spec, bool isRestored) {
            _lootTable = spec.lootTable.LootTable(spec);
            _requiredToolType = spec.ToolType;
            _overrideHealth = spec.overrideDefaultHealthTo3;
        }

        protected override void OnLocationFullyInitialized() {
            if (_alive == null) {
                return;
            }

            if (_overrideHealth) {
                _alive.MaxHealth.SetTo(DefaultHealth, false);
                _alive.Health.SetTo(DefaultHealth, false);
            }

            _alive.ListenTo(IAlive.Events.AfterDeath, DeathCallback, this);
        }

        void DeathCallback(DamageOutcome obj) {
            AddItemsToAttacker(obj.AttackerPure.Inventory);
            AbstractLocationAction.Interact(obj.AttackerPure, ParentModel);
            // AliveLocation handles its own discard in OnDeath
            if (!ParentModel.HasElement<AliveLocation>()) {
                ParentModel.Discard();
            } else {
                Discard();
            }
        }

        void AddItemsToAttacker(ICharacterInventory inventory) {
            IEnumerable<Item> items = Loot.Select(x => new Item(x));
            foreach (Item item in items) {
                CommitCrime.Theft(item, ParentModel);
                inventory.Add(item);
            }
        }

        public override string ModifyName(string original) {
            var displayedInfo = base.ModifyName(original);
            if (displayedInfo != original) {
                return displayedInfo;
            }

            if (original.IsNullOrWhitespace()) {
                Log.Important?.Error($"Fill DisplayName in {MainView.gameObject.name} - {ParentModel.Spec}!");
            }

            return original;
        }
    }
}