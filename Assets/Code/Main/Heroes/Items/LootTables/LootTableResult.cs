using System.Collections.Generic;
using System.Linq;

namespace Awaken.TG.Main.Heroes.Items.LootTables {
    public class LootTableResult {
        public IEnumerable<ItemSpawningDataRuntime> items = Enumerable.Empty<ItemSpawningDataRuntime>();
        
        public LootTableResult() { }
        public LootTableResult(IEnumerable<ItemSpawningDataRuntime> items) {
            this.items = items;
        }
        
        public LootTableResult Merge(LootTableResult other) => new LootTableResult(items.Concat(other.items));

        public LootTableResult Multiply(int amount) {
            var res = new LootTableResult(items.Select(i => 
                new ItemSpawningDataRuntime(i.ItemTemplate){quantity = i.quantity * amount}));
            return res;
        }
    }
}