using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Weapons;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.AI.Utils {
    public static class SummonUtils {
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public static NpcElement InitializeSummon(Location summon, ICharacter owner, Item item, float manaExpended, float manaCostPerSecond, [CanBeNull] IDuration duration) {
            NpcElement npc = summon.Element<NpcElement>();
            npc.OverrideFaction(owner.GetFactionTemplateForSummon(), FactionOverrideContext.Summon);

            INpcSummon npcSummon;
            if (owner is Hero hero) {
                npcSummon = npc.AddElement(new NpcHeroSummon(hero, item, manaExpended));
            } else {
                npcSummon = npc.AddElement(new NpcAISummon(owner, manaExpended));
            }

            if (duration != null) {
                npcSummon.AddElement(new CharacterLimitedLocationTimeoutAfterDuration(duration));
            }
            
            return npc;
        }

        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public static PlacedMine InitializeMine(Location mine, ICharacter owner, Item sourceItem, [CanBeNull] IDuration duration) {
            var placedMine = mine.AddElement(new PlacedMine(owner, sourceItem));

            if (duration != null) {
                placedMine.AddElement(new MineTimeoutAfterDuration(duration));
            }
            return placedMine;
        }
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public static void TriggerPlacedMine(Location mine) {
            mine.TryGetElement<PlacedMine>()?.Discard();
        }

        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public static void InitializePersistentAoE(Location location, ICharacter damageDealer) {
            location.AfterFullyInitialized(() => {
                location.TryGetElement<PersistentAoE>()?.AssignDamageDealer(damageDealer);
            });
        }
    }
}