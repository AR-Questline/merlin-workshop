using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("NPC/NPC: Give Item")]
    public class SEditorNpcGiveItem : EditorStep, IStoryActorRef {
        [Tooltip("Works only for actors added to story.")]
        public ActorRef actorRef;
        public ItemSpawningData itemData;
        public bool equip = true;
        
        public ActorRef[] ActorRef => new[] { actorRef };

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SNpcGiveItem {
                actorRef = actorRef,
                itemData = itemData,
                equip = equip
            };
        }
    }

    public partial class SNpcGiveItem : StoryStep {
        [Tooltip("Works only for actors added to story.")]
        public ActorRef actorRef;
        public ItemSpawningData itemData;
        public bool equip = true;

        public override StepResult Execute(Story story) {
            var actor = actorRef.Get();
            return StoryUtils.FindCharacter(story, actor, false) switch {
                Hero hero => GiveItem(story, hero.Inventory, item => equip && item.IsEquippable),
                NpcElement npc => GiveItem(story, npc.Inventory, item => equip && item.IsNPCEquippable && !item.IsWeapon),
                _ => LogWhenActorNotFound(story, actor)
            };
        }
        
        StepResult LogWhenActorNotFound(Story api, Actor actor) {
            Log.Important?.Warning($"Actor {actor.Id} ({actor.Name}) is not a valid character in {nameof(SEditorNpcGiveItem)} in the story {api.ID}");
            return StepResult.Immediate;
        }

        StepResult GiveItem(Story api, ICharacterInventory inventory, Predicate<Item> equipCondition) {
            if (TryCreateItem(api, out Item item)) {
                inventory.Add(item);
                
                if (equipCondition(item)) {
                    inventory.Equip(item);
                }
                
                return StepResult.Immediate;
            }
            
            return StepResult.Immediate;
        }
        
        bool TryCreateItem(Story api, out Item item) {
            item = null;
            
            if (itemData.ItemTemplate(api) == null) {
                Log.Important?.Error($"Null item assigned in NPC: Give Item in Story {api.ID}");
                return false;
            }
            
            item = new Item(itemData.ItemTemplate(api), itemData.quantity, itemData.ItemLvl);
            return true;
        }
    }
}