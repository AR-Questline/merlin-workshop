using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Editor.SceneCaches.Core;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;

namespace Awaken.TG.Editor.SceneCaches.Items {
    public class ItemsInGameCacheBaker : SceneBaker<ItemsInGameCache> {
        protected override ItemsInGameCache LoadCache => ItemsInGameCache.Get;

        List<ItemTemplate> _items = new(1000);

        public override void Bake(SceneReference scene) {
            SceneItemSources sceneSources = LootCache.Get.sceneSources.FirstOrDefault(s => s.sceneRef == scene);
            if (sceneSources != null) {
                foreach (var source in sceneSources.sources) {
                    foreach (var item in source.GetItems()) {
                        var template = item.Template;
                        if (template != null && !_items.Contains(template)) {
                            _items.Add(template);
                        }
                    }
                }
            }

            Cache.Editor_AfterBakeCleanUp();
        }

        public override void FinishBaking() {
            ItemsInGameCache.Get.allItemsInGame = _items
                .Select(i => new TemplateReference(i))
                .OrderBy(tr => tr.GUID)
                .ToArray();
            base.FinishBaking();
        }
    }
}