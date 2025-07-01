using Awaken.Utility;
using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Items {
    public partial class UnidentifiedItem : Element<Item>, IRefreshedByAttachment<UnidentifiedItemAttachment> {
        public override ushort TypeForSerialization => SavedModels.UnidentifiedItem;

        LootTableWrapper _lootTable;
        int _costOfIdentification;

        public int CostOfIdentification => _costOfIdentification;
        public ILootTable LootTable => _lootTable.LootTable(ParentModel);
        [UnityEngine.Scripting.Preserve] public LootTableWrapper LootTableWrapper => _lootTable;
        [JsonConstructor, UnityEngine.Scripting.Preserve] public UnidentifiedItem() { }

        public void InitFromAttachment(UnidentifiedItemAttachment spec, bool isRestored) {
            _lootTable = spec.LootTableWrapper;
            _costOfIdentification = spec.CostOfIdentification;
        }
        
        public IEnumerable<Item> Identify() {
            if (_lootTable == null) { 
                yield break;
            }

            foreach (var loot in LootTable.PopLoot(ParentModel).items) {
                ParentModel.Inventory.Add(new Item(loot));
                yield return new(loot);
            }
            ParentModel.DecrementQuantity();
        }
    }
}