using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.UI.UITooltips;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes.List;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Collections;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Hero/Hero: Change items quantity")]
    public class SEditorChangeItemsQuantity : EditorStep {
        // Tooltip is handled by SChangeItemsQuantityEditor
        [List(ListEditOption.FewButtons)]
        public List<ItemSpawningData> itemTemplateReferenceQuantityPairs = new();

        [Tags(TagsCategory.Item)] // Tooltip is handled by SChangeItemsQuantityEditor
        public string[] tags = Array.Empty<string>();
        [Tooltip("If positive value is given, items will be randomly added from all available in game with given tags. " +
                 "If negative value is given, items with given tags will be randomly taken from hero")]
        public int taggedQuantity;
        [Tags(TagsCategory.Item)] // Tooltip is handled by SChangeItemsQuantityEditor
        public string[] forbiddenTags = Array.Empty<string>();
        [Tooltip("Use this to allow player to choose items that he will receive from tags. Number means how many options should be showed at once.")]
        public ConditionalInt manualSelection = new (false, 3);
        [Tooltip("Should the player be allowed to cancel the action of giving item away.")]
        public bool allowCancel = true;

        [TemplateType(typeof(LootTableAsset))] // Tooltip is handled by SChangeItemsQuantityEditor
        public TemplateReference lootTableReference;

        [Tooltip("With this, choice leading to this action will show the consequences."), LabelText("Show Effect To Player")]
        public bool isKnown;
        [Tooltip("If player doesn't have required items to give away, should they be able to choose this branch?"), LabelText("Ignore Requirements")]
        public bool ignoreRequirements;
        [Tooltip("Check this if you want to take away all copies of the item from the player."), LabelText("Remove All Item Copies")]
        public bool removeAll;
        [Tooltip("Check this if you want to take away only stolen items from the player."), LabelText("Items Must Be Stolen")]
        public bool onlyStolen;
        
        int _changedAmount;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SChangeItemsQuantity {
                itemTemplateReferenceQuantityPairs = itemTemplateReferenceQuantityPairs.ToArray(),
                tags = tags,
                forbiddenTags = forbiddenTags,
                taggedQuantity = taggedQuantity,
                manualSelection = manualSelection,
                allowCancel = allowCancel,
                lootTableReference = lootTableReference,
                isKnown = isKnown,
                ignoreRequirements = ignoreRequirements,
                removeAll = removeAll,
                onlyStolen = onlyStolen,
                leaveChapter = parser.GetChapter(TargetNode() as ChapterEditorNode),
            };
        }
    }

    public partial class SChangeItemsQuantity : StoryStep {
        public ItemSpawningData[] itemTemplateReferenceQuantityPairs;
        public string[] tags;
        public string[] forbiddenTags;
        public TemplateReference lootTableReference;
        public ConditionalInt manualSelection = new (false, 3);
        public int taggedQuantity;
        public bool allowCancel = true;
        public bool isKnown;
        public bool ignoreRequirements;
        public bool removeAll;
        public bool onlyStolen;
        
        public StoryChapter leaveChapter;
        
        int _changedAmount;
        
        // === Execution
        public override StepResult Execute(Story story) {
            // assign correct hero
            Hero hero = story.Hero ?? Hero.Current;
            _changedAmount = 0;
            // give item
            // - from template
            itemTemplateReferenceQuantityPairs.ForEach(p => _changedAmount += ChangeItemQuantityBySpawningData(p.ToRuntimeData(story), hero, removeAll, story));
            
            // - from loot table
            var lootTable = lootTableReference?.Get<LootTableAsset>((story as Story)?.Guid);
            if (lootTable != null) {
                var loot = lootTable.PopLoot();
                foreach (var item in loot.items) {
                    _changedAmount += ChangeItemQuantityBySpawningData(item, hero, removeAll, story);
                }
            }

            // change tagged items quantity
            return ChangeTaggedItemsQuantity(story, hero);
        }
        
        int ChangeItemQuantityBySpawningData(ItemSpawningDataRuntime spawningData, Hero hero, bool removeAll, Story api) {
            if (spawningData.ItemTemplate != null) {
                if (removeAll && spawningData.quantity < 0) {
                    var numberOfItems = onlyStolen ? (int)hero.Inventory.NumberOfStolenItems(spawningData.ItemTemplate) : (int)hero.Inventory.NumberOfItems(spawningData.ItemTemplate);
                    spawningData.quantity = -1 * numberOfItems;
                }
                int quantityCache = Math.Abs(spawningData.quantity);
                spawningData.ChangeQuantity(hero.Inventory, onlyStolen);
                Announce(api, spawningData.ItemTemplate, spawningData.quantity);
                return quantityCache;
            }
            return 0;
        }

        public int GetChangedItemsQuantity() {
            return _changedAmount;
        }

        StepResult ChangeTaggedItemsQuantity(Story api, Hero hero) {
            if (taggedQuantity < 0) {
                return DecreaseTaggedItems(api, hero);
            } else if (taggedQuantity > 0) {
                return IncreaseTaggedItems(api, hero);
            }
            return StepResult.Immediate;
        }

        /// <summary>
        /// Needs to operate on StepResult, because it stops Story execution if there is choice to be made 
        /// </summary>
        StepResult DecreaseTaggedItems(Story api, Hero hero) {
            // qualified items with given tags
            List<Item> taggedItems = hero.Inventory.Items.Where(item => !item.HiddenOnUI && StolenConditionMet(item) && TagUtils.HasRequiredTags(item, tags) && TagUtils.DoesNotHaveForbiddenTags(item, forbiddenTags)).ToList();

            if (removeAll) {
                List<Item> qualifiedItems = hero.Inventory.Items.Where(item => !item.HiddenOnUI && StolenConditionMet(item) && TagUtils.HasRequiredTags(item, tags) && TagUtils.DoesNotHaveForbiddenTags(item, forbiddenTags)).ToList();
                taggedQuantity = -1 * qualifiedItems.Sum(static item => item.Quantity);
            }

            _changedAmount -= taggedQuantity;
            
            // choice is offered only when there is only 1 item to take
            if (taggedQuantity == -1) {
                // offer choice (which item to remove?)
                StepResult result = new StepResult();
                api.ShowText(TextConfig.WithText(LocTerms.StoryPickItem.Translate()));
                foreach (Item item in taggedItems.DistinctBy(i => i.Template)) {
                    OfferItemChoice(api, item, result);
                }
                if (allowCancel || taggedItems.Count == 0) {
                    OfferLeaveChoice(api, result);
                }

                return result;
            } else {
                // randomly remove given number of items
                hero.Inventory.ChangeItemQuantityByTags(tags, taggedQuantity, (t, q) => Announce(api, t, q), onlyStolen, forbiddenTags);
                return StepResult.Immediate;
            }
        }

        StepResult IncreaseTaggedItems(Story api, Hero hero) {
            _changedAmount += taggedQuantity;
            
            if (manualSelection) {
                StepResult result = new StepResult();
                IncreaseTaggedItemsRecur(api, hero, 0, result);
                return result;
            }

            hero.Inventory.ChangeItemQuantityByTags(tags, taggedQuantity, (t, q) => Announce(api, t, q));
            return StepResult.Immediate;
        }

        void IncreaseTaggedItemsRecur(Story api, Hero hero, int iteration, StepResult result) {
            if (iteration >= taggedQuantity) {
                result.Complete();
                return;
            }

            api.ShowText(TextConfig.WithText(LocTerms.StoryPickItem.Translate()));
            
            var toSelect = (int) manualSelection;
            IEnumerable<ItemTemplate> taggedItems;
            if (toSelect > 0) {
                taggedItems = ItemUtils.GetRandomItemsByTag(tags, toSelect);
            } else {
                taggedItems = ItemUtils.GetRandomItemsByTag(tags);
            }
        
            taggedItems.ForEach(i => OfferItemChoice(api, i, hero, () => IncreaseTaggedItemsRecur(api, hero, iteration+1, result)));
        }

        void OfferItemChoice(Story api, Item item, StepResult result) {
            string currentlyEquipped = item.IsEquipped ? $" ({LocTerms.StoryCurrentlyEquipped.Translate()})" : "";
            string id = World.Services.Get<UITooltipStorage>().Register(item, "description", item.Flavor, api);
            string text = $"{LocTerms.StoryGiveItem.Translate(item.DisplayName, item.Inventory.NumberOfItems(item.Template)).AddTooltip(id)}{currentlyEquipped}";
            var choice = new RuntimeChoice {
                text = (LocString) text,
                Tooltip = item.Flavor
            };

            void TakeItem() {
                api.Clear();
                item.DecrementQuantity();
                result.Complete();
                Announce(api, item.Template, -1);
            }

            api.OfferChoice(ChoiceConfig.WithCallback(choice, TakeItem).WithSpriteIcon(item.Icon));
        }
        
        void OfferItemChoice(Story api, ItemTemplate itemTemplate, Hero hero, Action onChoose) {
            World.Services.Get<UITooltipStorage>().Register(itemTemplate.DescriptionLoc.ID, ItemUtils.GetTemplateDescription(itemTemplate, hero), api);
            string text = $"{LocTerms.StoryGetItem.Translate(itemTemplate.ItemName, hero.Inventory.NumberOfItems(itemTemplate)).AddTooltip(itemTemplate.DescriptionLoc.ID)}";
            var choice = new RuntimeChoice {
                text = (LocString) text,
                Tooltip = ItemUtils.CreateItemFlavorFromTemplate(itemTemplate, api.Hero)
            };

            void TakeItem() {
                api.Clear();
                itemTemplate.ChangeQuantity(hero.Inventory, 1);
                Announce(api, itemTemplate, 1);
                onChoose();
            }

            api.OfferChoice(ChoiceConfig.WithCallback(choice, TakeItem).WithSpriteIcon(itemTemplate.IconReference));
        }

        void OfferLeaveChoice(Story api, StepResult result) {
            var choice = new RuntimeChoice {
                text = (LocString) LocTerms.Leave.Translate(),
                targetChapter = leaveChapter,
            };

            void Leave() {
                api.Clear();
                result.Complete();
                api.JumpTo(choice.targetChapter);
            }
            
            api.OfferChoice(ChoiceConfig.WithCallback(choice, Leave));
        }
        
        // === Meta
        
        public override void AppendKnownEffects(Story story, ref StructList<string> effects) {
            if (!isKnown) {
                return;
            }
            
            // pairs
            foreach (var pair in itemTemplateReferenceQuantityPairs) {
                ItemTemplate template = pair.ItemTemplate(story);
                if (template != null) {
                    string desc = ItemUtils.GetTemplateDescription(template, story.Hero);
                    World.Services.Get<UITooltipStorage>().Register(template.GUID, desc, story);
                    
                    string sign = pair.quantity > 0 ? "+" : "-";
                    string itemName = $"{template.ItemName}".AddTooltip(template.GUID);
                    int quantity = (removeAll && pair.quantity < 0) ? Math.Max(GetAllRequiredItemsCount(story, template), -pair.quantity) : Math.Abs(pair.quantity);
                    effects.Add($"{sign}{LocTerms.ItemWithQuantity.Translate(quantity, itemName)}");
                }
            }

            // tags
            if (tags != null && tags.Any() && taggedQuantity != 0) {
                string aggregatedTags = string.Join(", ", tags.Select(t => ExtractTagText(t)));
                aggregatedTags = string.IsNullOrWhiteSpace(aggregatedTags) ? "" : $"{aggregatedTags}";
                int quantity = (removeAll && taggedQuantity < 0) ? Math.Max(GetAllTaggedItemsCount(story), -taggedQuantity) : taggedQuantity;
                effects.Add($"{quantity} {aggregatedTags}");
            }
        }

        public override string GetKind(Story story) {
            return "Story";
        }
        
        public override StepRequirement GetRequirement() {
            return api => {
                bool hasAnyNegativeQuantity = taggedQuantity < 0 || itemTemplateReferenceQuantityPairs.Any(p => p.quantity < 0);
                return !hasAnyNegativeQuantity || ignoreRequirements || StoryUtils.HasRequiredItems(itemTemplateReferenceQuantityPairs.Where(iqp => iqp.quantity < 0), tags, taggedQuantity, true, onlyStolen, false, forbiddenTags);
            };
        }
        
        int GetAllTaggedItemsCount(Story api) {
            int count = api.Hero.Inventory.Items.Where(item => StolenConditionMet(item) && TagUtils.HasRequiredTags(item, tags))
                .Sum(static item => item.Quantity);
            return (count == 0) ? 1 : count;
        }

        int GetAllRequiredItemsCount(Story api, Template template) {
            return api.Hero.Inventory.Items.FirstOrDefault(i => StolenConditionMet(i) && i.Template == template)?.Quantity ?? 1;
        }

        string ExtractTagText(string tag, bool useValueAsFallback = false) {
            if (string.IsNullOrWhiteSpace(tag)) {
                return "";
            }
            string tagString = "";
            if (!string.IsNullOrWhiteSpace(tag)) {
                string term = TagUtils.GetTagID(tag);
                string fallback = useValueAsFallback ? TagUtils.TagValue(tag).Capitalize() : "";
                tagString = term.TranslateWithFallback(fallback);
            }
            return tagString;
        }
        
        bool StolenConditionMet(Item item) {
            return !onlyStolen || item.IsStolen;
        }
        
        /// <summary>
        /// Announce item quantity change to world
        /// </summary>
        static void Announce(Story api, ItemTemplate itemTemplate, int quantity) {
            string tooltip = ItemUtils.GetTemplateDescription(itemTemplate, api.Hero);
            World.Services.Get<UITooltipStorage>().Register(itemTemplate.ItemName, tooltip, owner: api);
        }
    }
}
