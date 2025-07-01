using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;

namespace Awaken.TG.Main.General.Caches {
    public class ItemsInGameCache : BaseCache {
        static ItemsInGameCache s_cache;
        public static ItemsInGameCache Get => s_cache = s_cache ? s_cache : LoadFromResources<ItemsInGameCache>("Caches/ItemsInGameCache");

        public TemplateReference[] allItemsInGame = Array.Empty<TemplateReference>();

        public override void Clear() {
            allItemsInGame = Array.Empty<TemplateReference>();
        }
        
        HashSet<TemplateReference> _set;
        HashSet<TemplateReference> Set => _set ??= allItemsInGame.ToHashSet();
        public bool Editor_HasAnyOccurrencesOf(ItemTemplate itemTemplate) => Set.Contains(new TemplateReference(itemTemplate));
        public void Editor_AfterBakeCleanUp() => _set = allItemsInGame.ToHashSet();
    }
}