using System;
using Awaken.TG.Assets;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Fights.Duels {
    [Serializable]
    public struct DuelistSettings {
        public bool fightToDeath;
        [HideIf(nameof(fightToDeath))] public bool canBeTalkedToDefeated;
        public bool keepsDuelAlive; // The group is defeated when all characters keeping it alive are defeated
        public bool restoreHealthOnStart;
        public bool restoreHealthOnEnd;
        
        [AnimancerAnimationsAssetReference] 
        public ARAssetReference defeatedAnimationsOverrides;

        public static DuelistSettings Default => new() {
            fightToDeath = false,
            canBeTalkedToDefeated = true,
            keepsDuelAlive = true,
            restoreHealthOnStart = true,
            restoreHealthOnEnd = true,
        };
        
        public static DuelistSettings Summon => new() {
            fightToDeath = true,
            canBeTalkedToDefeated = false,
            keepsDuelAlive = false,
            restoreHealthOnStart = false,
            restoreHealthOnEnd = false,
        };
    }
}