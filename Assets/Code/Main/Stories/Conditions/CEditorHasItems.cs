using System;
using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Utility.Attributes.List;
using Awaken.TG.Utility.Attributes.Tags;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Conditions {
    /// <summary>
    /// Check if hero has enough items
    /// </summary>
    [Element("Hero: Has items")]
    public class CEditorHasItems : EditorCondition {
        [Tooltip("Use this to specify which items and how many of them are required")] [List(ListEditOption.Buttons)]
        public List<ItemSpawningData> requiredItemTemplateReferenceQuantityPairs = new();

        [Tooltip("Use this to require X items with given tags"), Tags(TagsCategory.Item)]
        public string[] tags = Array.Empty<string>();

        [Tooltip("Define the quantity of items the hero needs to meet this condition.")]
        public int tagsQuantity = 1;

        [Tooltip("Use this to forbid items from having given tags"), Tags(TagsCategory.Item)]
        public string[] forbiddenTags = Array.Empty<string>();

        [Tooltip("Check this if you want to take into account only stolen items."), LabelText("Only Stolen")]
        public bool onlyStolen;

        [Tooltip("Check this if you want to take into account only currently equipped items."), LabelText("Only Equipped")]
        public bool onlyEquipped;

        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CHasItems {
                requiredItemTemplateReferenceQuantityPairs = requiredItemTemplateReferenceQuantityPairs.ToArray(),
                tags = tags,
                tagsQuantity = tagsQuantity,
                forbiddenTags = forbiddenTags,
                onlyStolen = onlyStolen,
                onlyEquipped = onlyEquipped
            };
        }
    }

    public partial class CHasItems : StoryCondition {
        public ItemSpawningData[] requiredItemTemplateReferenceQuantityPairs;
        public string[] tags;
        public int tagsQuantity;
        public string[] forbiddenTags;
        public bool onlyStolen;
        public bool onlyEquipped;
        
        public override bool Fulfilled(Story story, StoryStep step) {
            return StoryUtils.HasRequiredItems(requiredItemTemplateReferenceQuantityPairs, tags, tagsQuantity, false, onlyStolen, onlyEquipped, forbiddenTags);
        }
    }
}