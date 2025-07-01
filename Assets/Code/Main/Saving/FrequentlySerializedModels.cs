using System;
using System.Collections.Generic;
using Awaken.TG.MVC;
using Awaken.Utility;

namespace Awaken.TG.Main.Saving {
    public static class FrequentlySerializedModels {
        public static Dictionary<Type, string> FullTypeStringToShortTypeStringMap = new() {
            { typeof(Locations.Location), "1" },
            { typeof(Locations.Actions.SearchAction), "2" },
            { typeof(Heroes.Items.Item), "3" },
            { typeof(Heroes.Items.Attachments.ItemEquip), "4" },
            { typeof(Heroes.Items.Weapons.ItemStats), "5" },
            { typeof(Heroes.Items.Attachments.ItemEffects), "6" },
            { typeof(Heroes.Items.Attachments.Audio.ItemAudio), "7" },
            { typeof(Heroes.Items.Weapons.ItemStatsRequirements), "8" },
            { typeof(Heroes.Development.Talents.Talent), "9" },
            { typeof(AI.Idle.Data.Runtime.IdleDataElement), "10" },
            { typeof(Character.Features.BodyFeatures), "11" },
            { typeof(Character.AliveStats), "12" },
            { typeof(Character.CharacterStats), "13" },
            { typeof(Character.StatusStats), "14" },
            { typeof(Skills.CharacterSkills), "15" },
            { typeof(Heroes.Statuses.CharacterStatuses), "16" },
            { typeof(Fights.NPCs.NpcItems), "17" },
            { typeof(Fights.NPCs.NpcElement), "18" },
            { typeof(Character.NpcStats), "19" },
            { typeof(AI.Idle.Behaviours.IdleBehaviours), "20" },
            { typeof(Timing.ARTime.ARTimeProvider), "21" },
            { typeof(AI.Movement.Controllers.ARDeltaPositionLimiter), "22" },
            { typeof(Fights.NPCs.Providers.NpcIsGroundedHandler), "23" },
            { typeof(Maps.Markers.NpcMarker), "24" },
            { typeof(Skills.Skill), "25" },
            { typeof(AI.Combat.Attachments.Humanoids.HumanoidCombat), "26" },
            { typeof(Fights.NPCs.Presences.NpcPresence), "27" },
            { typeof(AI.Barks.BarkElement), "29" },
            { typeof(Heroes.Stats.StatTweak), "30" },
            { typeof(Locations.Actions.DialogueAction), "31" },
            { typeof(Locations.Attachments.Elements.Portal), "32" },
            { typeof(Heroes.Items.Attachments.Audio.AliveAudio), "33" },
            { typeof(Maps.Markers.DiscoveryMarker), "34" },
            { typeof(Heroes.Items.Attachments.ItemProjectile), "35" },
            { typeof(Locations.Discovery.LocationDiscovery), "36" },
            { typeof(AI.Combat.Attachments.Customs.CustomCombatBaseClass), "37" },
            { typeof(Locations.Attachments.Elements.KillPreventionElement), "38" },
            { typeof(Locations.Actions.PickItemAction), "39" },
            { typeof(Locations.Spawners.LocationSpawner), "40" },
            { typeof(Locations.Actions.LockAction), "41" },
            { typeof(Locations.Shops.MerchantStats), "42" },
            { typeof(Locations.Shops.Shop), "43" },
            { typeof(Locations.Shops.Stocks.UniqueStock), "44" },
            { typeof(Locations.Shops.Stocks.BoughtFromHeroStock), "45" },
            { typeof(Locations.Shops.Stocks.RestockableStock), "46" },
            { typeof(Locations.Shops.Prices.CachedPriceProvider), "47" },
            { typeof(Heroes.Items.Attachments.ItemRead), "48" },
            { typeof(Saving.SaveSlots.SaveSlot), "49" },
            { typeof(Heroes.Items.Buffs.ItemThrowable), "50" },
            { typeof(Utility.Animations.AnimatorElement), "51" },
            { typeof(Heroes.Items.ItemSlot), "52" },
            { typeof(Locations.Spawners.GroupLocationSpawner), "53" },
            { typeof(Locations.Attachments.Elements.LocationWithNpcInteractionElement), "54" },
            { typeof(Locations.Attachments.Elements.BedElement), "55" },
            { typeof(Maps.Markers.LocationMarker), "56" },
            { typeof(Heroes.Development.Talents.TalentTable), "57" },
            { typeof(Heroes.Items.Attachments.Tool), "58" }
        };

        public static Dictionary<string, Type> ShortTypeStringToFullTypeStringMap = FullTypeStringToShortTypeStringMap.InvertDictionary();

        public static Dictionary<string, Func<Model>> ShortTypeToNewObject = new() {
            { "1", Locations.Location.JsonCreate },
            { "2", Locations.Actions.SearchAction.JsonCreate },
            { "3", Heroes.Items.Item.JsonCreate },
            { "4", Heroes.Items.Attachments.ItemEquip.JsonCreate },
        };
    }
}