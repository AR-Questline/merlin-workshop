using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
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
    public sealed partial class LootInteractAction : ToolInteractAction, IRefreshedByAttachment<LootInteractAttachment> {
        public override ushort TypeForSerialization => SavedModels.LootInteractAction;

        const int DefaultHealth = 3;
        
        ILootTable _lootTable;

        public IEnumerable<ItemSpawningDataRuntime> Loot =>
            ItemUtils.GetItemSpawningDataFromLootTable(_lootTable, ParentModel.Spec, this).Where(x => x?.ItemTemplate != null);

        public override bool IsIllegal => Loot.TryGetFirst(out var data) && Crime.Theft(data, ParentModel).IsCrime();

        public void InitFromAttachment(LootInteractAttachment spec, bool isRestored) {
            _lootTable = spec.lootTable.LootTable(spec);
            _requiredToolType = spec.ToolType;
        }

        protected override void OnLocationFullyInitialized() {
            if (_alive == null) {
                return;
            }
            
            _alive.MaxHealth.SetTo(DefaultHealth, false);
            _alive.Health.SetTo(DefaultHealth, false);
            _alive.ListenTo(IAlive.Events.AfterDeath, DeathCallback, this);
        }

        void DeathCallback(DamageOutcome obj) {
            AddItemsToAttacker(obj.Attacker.Inventory);
            AbstractLocationAction.Interact(obj.Attacker, ParentModel);
            ParentModel.Discard();
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