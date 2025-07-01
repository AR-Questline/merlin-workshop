using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Fights.NPCs {
    public enum NpcType : byte {
        Critter,
        Trash,
        Normal,
        Elite,
        MiniBoss,
        Boss
    }

    public static class NpcTypeUtils {
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public static bool IgnoreSpecialEffects(this NpcElement npc) => IgnoreSpecialEffects(npc.NpcType);

        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public static bool IgnoreSpecialEffects(this NpcType type) {
            switch (type) {
                case NpcType.Critter:
                case NpcType.Trash:
                case NpcType.Normal:
                case NpcType.Elite:
                case NpcType.MiniBoss:
                    return false;
                case NpcType.Boss:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public static bool IsOneOfTypes(this NpcElement npc, List<NpcType> types) => IsOneOfTypes(npc.NpcType, types);
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public static bool IsOneOfTypes(this NpcType type, List<NpcType> types) => types.Contains(type);
    }
}
