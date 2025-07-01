using System;
using System.Linq;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes.Tags;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.ItemBasedBehaviours {
    [Serializable]
    public partial class ItemRequiringBehaviourForwarder : EnemyBehaviourForwarder {
        [SerializeField] ItemSelection itemSelection;
        [SerializeField, ShowIf(nameof(ShowRequiredItem))] ItemSpawningData requiredItem;
        [SerializeField, ShowIf(nameof(ShowRequiredItemTags)), HideLabel, Tags(TagsCategory.Item)] string[] requiredItemTags = Array.Empty<string>();
        [SerializeField, ShowIf(nameof(ShowRequiredItemAbstracts)), TemplateType(typeof(ItemTemplate))] TemplateReference requiredItemAbstract;
        [SerializeReference] EnemyBehaviourBase behaviour;

        Item _cachedItem;
        
        bool ShowRequiredItem => itemSelection == ItemSelection.SpecificItem;
        bool ShowRequiredItemTags => itemSelection == ItemSelection.AnyItemWithTags;
        bool ShowRequiredItemAbstracts => itemSelection == ItemSelection.AnyItemWithAbstracts;
        
        public Item ItemItsAddedTo { get; set; }
        
        protected override EnemyBehaviourBase BehaviourToClone {
            get => behaviour;
            set => behaviour = value;
        }

        protected override bool AdditionalConditions => HasItem;
        protected bool HasItem => Item != null;
        protected Item Item => _cachedItem;
        
        protected override void OnInitialize() {
            base.OnInitialize();
            _cachedItem = GetItem();
            Npc.Inventory.ListenTo(ICharacterInventory.Relations.Contains.Events.Changed, OnEquipmentChanged, Npc);
        }
        
        void OnEquipmentChanged() {
            if (_cachedItem is { HasBeenDiscarded: false } && _cachedItem.Owner == Npc) {
                return;
            }
            _cachedItem = GetItem();
        }

        Item GetItem() => itemSelection switch {
            ItemSelection.SpecificItem => Npc.Inventory.Items.FirstOrDefault(i => i.Template == requiredItem.ItemTemplate(Npc)),
            ItemSelection.AnyItemWithTags => Npc.Inventory.Items.FirstOrDefault(i => TagUtils.HasRequiredTags(i, requiredItemTags)),
            ItemSelection.AnyItemWithAbstracts => Npc.Inventory.Items.FirstOrDefault(i => i.Template.InheritsFrom(requiredItemAbstract.Get<ItemTemplate>())),
            ItemSelection.ItemItsAddedTo => ItemItsAddedTo,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        internal enum ItemSelection {
            SpecificItem,
            AnyItemWithTags,
            AnyItemWithAbstracts,
            ItemItsAddedTo
        }
    }
}
