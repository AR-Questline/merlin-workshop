using System.Linq;
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
    [Element("NPC/NPC: Remove Item")]
    public class SEditorNpcRemoveItem : EditorStep, IStoryActorRef {
        [Tooltip("Works only for actors already added to Story.")]
        public ActorRef actorRef;
        public ItemSpawningData itemData;
        public ActorRef[] ActorRef => new[] { actorRef };

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SNpcRemoveItem {
                actorRef = actorRef,
                itemData = itemData
            };
        }
    }
    
    public partial class SNpcRemoveItem : StoryStep {
        public ActorRef actorRef;
        public ItemSpawningData itemData;
        
        public override StepResult Execute(Story story) {
            return StoryUtils.FindCharacter(story, actorRef.Get(), false) switch {
                Hero hero => RemoveItem(hero.Inventory),
                NpcElement npc => RemoveItem(npc.Inventory),
                _ => LogWhenActorNotFound(story)
            };
        }
        
        StepResult LogWhenActorNotFound(Story api) {
            Log.Important?.Warning($"Actor {actorRef.guid} is not a valid character in {nameof(SEditorNpcGiveItem)} in the story {api.ID}");
            return StepResult.Immediate;
        }

        StepResult RemoveItem(ICharacterInventory inventory) {
            if (TryGetItem(inventory, itemData, out Item item)) {
                ItemUtils.RemoveItem(item, itemData.quantity == 0 ? 1 : Mathf.Abs(itemData.quantity));
                return StepResult.Immediate;
            }
            return StepResult.Immediate;
        }
        
        public static bool TryGetItem(IInventory inventory, ItemSpawningData itemData, out Item item) {
            item = inventory.Items.FirstOrDefault(i => i.Template == itemData.ItemTemplate(null));
            return item;
        }
    }
}