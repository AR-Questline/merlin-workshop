using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.Utility.Debugging;
using Awaken.Utility.Enums;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Humanoids {
    [RichEnumAlwaysDisplayCategory]
    public class FighterType : RichEnum {
        public static readonly FighterType
            None = new(nameof(None)),
            OneHanded = new(nameof(OneHanded), "Melee"),
            OneHandedDagger = new(nameof(OneHandedDagger), "Melee"),
            TwoHanded = new(nameof(TwoHanded), "Melee"),
            Fists = new(nameof(Fists), "Melee"),
            Ranged = new(nameof(Ranged), "Ranged"),
            DualWielding = new(nameof(DualWielding), "Melee"),
            HeavyDualWielding = new(nameof(HeavyDualWielding), "Melee");

        // === Constructor
        protected FighterType(string enumName, string inspectorCategory = "") : base(enumName, inspectorCategory) { }

        // === Public API
        public static FighterType GetFighterType(NpcElement npc, Item mainHandItem, Item offHandItem, Location debugTarget) {
            if (mainHandItem is { IsTwoHanded: true }) {
                if (mainHandItem.IsRanged) {
                    return Ranged;
                }
                if (mainHandItem.IsFists) {
                    return Fists;
                }
                return TwoHanded;
            }
            
            if (mainHandItem != null && offHandItem != null) {
                if (mainHandItem.IsDagger && offHandItem.IsDagger) {
                    return DualWielding;
                }
                return HeavyDualWielding;
            }
            
            if (mainHandItem != null) {
                return mainHandItem.IsDagger ? OneHandedDagger : OneHanded;
            }
            
            Log.Important?.Error("Failed to determine FighterType! " + LogUtils.GetDebugName(debugTarget), debugTarget.MainView);
            return None;
        }
    }
}