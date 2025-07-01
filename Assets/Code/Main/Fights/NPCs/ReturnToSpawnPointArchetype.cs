namespace Awaken.TG.Main.Fights.NPCs {
    public enum ReturnToSpawnPointArchetype : byte {
        /// <summary> Can be kited away from spawn point. </summary>
        [UnityEngine.Scripting.Preserve] Offensive = 0,
        /// <summary>  Can chase to max distance from spawn point, never exceeded. </summary>
        [UnityEngine.Scripting.Preserve] Defensive = 1,
        /// <summary>  Can chase to max distance from spawn point, never exceeded. </summary>
        [UnityEngine.Scripting.Preserve] DefensiveWithoutHealing = 2,
        /// <summary>  Ignores Return to Spawn Point state and jumps straight to Idle. </summary>
        [UnityEngine.Scripting.Preserve] UseIdleInstant = 3,
    }
}