using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awaken.TG.Editor.Assets.Templates;
using Awaken.TG.Main.Heroes.Items;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.Graphics.IconRenderer {
    [CreateAssetMenu(fileName = nameof(IconRendererSettings), menuName = nameof(IconRendererSettings), order = 0), InlineEditor]
    public class IconRendererSettings : ScriptableObject {
        const string SelectionButtonGroup = "select";
        const string FindingButtonGroup = "find";

        [FolderPath] public string outputPath = "Assets/2DAssets/RawRenderedIcons/Output";

        public RenderTexture renderTexture;
        [Searchable, ListDrawerSettings(CustomAddFunction = nameof(CustomAddCategory))] public List<IconRendererCategory> categories;

        [SerializeField] IconRendererCategory defaultCategory = new("Default");
        
        public IconRendererCategory FindCategory(GameObject prefab) {
            IconRendererCategory result = null;
            foreach (var category in categories) {
                var iconRenderingSettings = category.FindIconRenderingSettings(prefab);
                if (iconRenderingSettings != null) {
                    result = category;
                }
            }

            bool foundInDefault = defaultCategory.FindIconRenderingSettings(prefab) != null;
            if (result != null) {
                if (foundInDefault) {
                    defaultCategory.Remove(prefab);
                    return result;
                }
                return result;
            }

            if (!foundInDefault) {
                defaultCategory.Add(prefab);
            }
            
            return defaultCategory;
        }

        public IEnumerable<IconRendererCategory> GetCategories() {
            yield return defaultCategory;
            foreach (var category in categories) {
                yield return category;
            }
        }

        [Button, ButtonGroup(SelectionButtonGroup)]
        public void UseNone() {
            foreach (var entry in categories) {
                entry.use = false;
            }
        }

        [Button, ButtonGroup(SelectionButtonGroup)]
        void UseAll() {
            foreach (var entry in categories) {
                entry.use = true;
            }
        }

        [Button, ButtonGroup(FindingButtonGroup)]
        void CheckForDuplicates() {
            Dictionary<IconRenderingSettings, int> counts = new();
            var itemTemplates = categories.SelectMany(tc => tc.IconsRenderingSettings);
            foreach (var itemTemplate in itemTemplates) {
                if (itemTemplate != null) {
                    if (!counts.TryAdd(itemTemplate, 1)) {
                        counts[itemTemplate]++;
                    }
                }
            }

            StringBuilder stringBuilder = new();
            foreach (var duplicate in counts.Where(c => c.Value > 1)) {
                stringBuilder.Append($"{duplicate.Key} found {duplicate.Value.ToString()} times");
                foreach (IconRendererCategory category in categories) {
                    int count = category.IconsRenderingSettings.Count(i => i == duplicate.Key);
                    if (count > 0) {
                        stringBuilder.Append($"\n\tin {category.category}: {count.ToString()} times");
                    }
                }

                Log.Important?.Warning(stringBuilder.ToString());
                stringBuilder.Clear();
            }
        }

        [Button, ButtonGroup(FindingButtonGroup)]
        void FindNew() {
            var settings = categories.SelectMany(tc => tc.IconsRenderingSettings);
            var newTemplates = TemplatesSearcher
                .FindAllOfType<IconRenderingSettings>()
                .Except(settings)
                .Where(it => !(it.prefab.GetComponent<ItemTemplate>() != null && it.prefab.GetComponent<ItemTemplate>().IsAbstract));

            Log.Important?.Info($"{nameof(IconRendererWindow)} doesn't contain item templates:");
            foreach (var itemTemplate in newTemplates) {
                Log.Important?.Info($"\t{itemTemplate}", itemTemplate.prefab);
            }
        }

        [Button, ButtonGroup(FindingButtonGroup)]
        void ClearNullItems() {
            foreach (var category in categories) {
                category.IconsRenderingSettings.RemoveAll(item => item == null);
            }

            categories.RemoveAll(c => c.IconsRenderingSettings.Count == 0);
        }

        [Button, ButtonGroup(FindingButtonGroup)]
        void SortCategories() {
            categories = categories.OrderBy(c => c.category).ToList();
        }
        
        IconRendererCategory CustomAddCategory() {
            var category = new IconRendererCategory("New Category")
            {
                transform = TransformValues.Default
            };
            return category;
        }
    }
}