using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Tags;
using Awaken.Utility.Debugging;
using TMPro;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Stories.Debugging.DebugSetupComponents {
    public class DebugSetItem : MonoBehaviour, IDebugComponent {
        public TextMeshProUGUI textComponent;
        public TMP_InputField count;

        ItemTemplate _template;
        string[] _tags;

        Item FindItemWithTemplate(Hero hero) => hero.Inventory.Items.FirstOrDefault(item => item.Template == _template);
        IEnumerable<Item> ItemsWithTags(Hero hero, string[] tags) => hero.Inventory.Items.Where(i => TagUtils.HasRequiredTags(i, tags));
        int SumOfItemsWithTags(Hero hero, string[] tags) => ItemsWithTags(hero, tags).Sum(i => i.Quantity);

        public void Init(Story story, ItemTemplate itemTemplate, int quantity) {
            _template = itemTemplate;
            textComponent.text = $"{_template.itemName} ({quantity})";
            count.text = FindItemWithTemplate(story.Hero)?.Quantity.ToString() ?? "0";
        }
        
        public void Init(Story story, string[] tags, int quantity) {
            _tags = tags;
            textComponent.text = $"{string.Join(", ", _tags)} ({quantity})";
            count.text = SumOfItemsWithTags(story.Hero, tags).ToString();
        }

        public void Apply(Story story) {
            ApplyForTemplate(story);
            ApplyForTags(story);
        }

        void ApplyForTemplate(Story story) {
            if (_template == null) {
                return;
            }
            
            if (!int.TryParse(count.text, out int quantity)) {
                Log.Important?.Error($"Invalid number for item {textComponent.text}");
                return;
            }
            
            Item item = FindItemWithTemplate(story.Hero);
            int diff = quantity - (item?.Quantity ?? 0);
            _template.ChangeQuantity(story.Hero.Inventory, diff);
        }

        void ApplyForTags(Story story) {
            if (_tags == null || !_tags.Any()) {
                return;
            }
            
            if (!int.TryParse(count.text, out int quantity)) {
                Log.Important?.Error($"Invalid number for item {textComponent.text}");
                return;
            }
            
            int currentCount = SumOfItemsWithTags(story.Hero, _tags);
            int diff = quantity - currentCount;
            
            story.Hero.Inventory.ChangeItemQuantityByTags(_tags, diff);
        }
    }
}