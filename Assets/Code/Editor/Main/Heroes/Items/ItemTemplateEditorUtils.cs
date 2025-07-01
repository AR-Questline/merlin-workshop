using System.Collections.Generic;
using Awaken.TG.Editor.Assets.Templates;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using UnityEditor;

namespace Awaken.TG.Editor.Main.Heroes.Items {
    public static class ItemTemplateEditorUtils {
        [MenuItem("TG/Build/Baking/Bake Item Descriptions")]
        static void BakeDebugDescriptions() {
            List<ItemTemplate> templates = TemplatesSearcher.FindAllOfType<ItemTemplate>();
            for (int i = 0; i < templates.Count; i++) {
                var template = templates[i];
                Log.Important?.Info($"[Item Description] {i}/{templates.Count} - {template.name}", template);
                BakeDebugDescription(template);
            }
            AssetDatabase.SaveAssets();
        }
        
        static void BakeDebugDescription(ItemTemplate template) {
            var accessor = new ItemTemplate.EditorAccessor(template);
            var item = new Item(template);
            World.Add(item);
            Hero.Current.Inventory.Add(item);
            accessor.BakedDescription = item.DescriptionFor(Hero.Current);
            EditorUtility.SetDirty(template);
        }
    }
}