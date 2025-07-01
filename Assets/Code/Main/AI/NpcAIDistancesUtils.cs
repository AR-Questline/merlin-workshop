namespace Awaken.TG.Main.AI {
    public static class NpcAIDistancesUtils {
        // --- Distance
        const float ReturnToSpawnBand0 = 15;
        const float ReturnToSpawnBand1 = 30;
        public const float ReturnToSpawnBand2 = 50;
        const float ReturnToSpawnBand3 = 85;
        const float ReturnToSpawnBand4 = 150;
        const float SqrReturnToSpawnBand0 = ReturnToSpawnBand0 * ReturnToSpawnBand0;
        const float SqrReturnToSpawnBand1 = ReturnToSpawnBand1 * ReturnToSpawnBand1;
        const float SqrReturnToSpawnBand2 = ReturnToSpawnBand2 * ReturnToSpawnBand2;
        const float SqrReturnToSpawnBand3 = ReturnToSpawnBand3 * ReturnToSpawnBand3;
        const float SqrReturnToSpawnBand4 = ReturnToSpawnBand4 * ReturnToSpawnBand4;
        // --- Speed
        const float Band1SpeedModifier = 1.15f;
        const float Band2SpeedModifier = 1.55f;
        // --- Alert
        const float Band1NewAlertModifier = 0.85f;
        const float Band2NewAlertModifier = 0.7f;
        const float Band1AlertDecreaseModifier = 1.5f;
        const float Band2AlertDecreaseModifier = 3.33f;
        // --- Hero Visibility
        const float Band1MinHeroVisibilityModifier = 0.25f;
        const float Band2MinHeroVisibilityModifier = 0.65f;
        const float Band0LoseHeroDelay = 3.0f;
        const float Band1LoseHeroDelay = 2.5f;
        const float Band2LoseHeroDelay = 1.0f;
        // --- Aggro
        const float Band0AggroDecreaseModifier = 0.05f;
        const float Band1AggroDecreaseModifier = 0.2f;
        const float Band2AggroDecreaseModifier = 0.5f;

        public static bool IsInBand0(this NpcAI npcAI) {
            return npcAI.SqrDistanceToOutOfCombatPoint <= SqrReturnToSpawnBand0;
        }
        
        public static bool IsInBand1(this NpcAI npcAI) {
            return npcAI.SqrDistanceToOutOfCombatPoint <= SqrReturnToSpawnBand1;
        }

        public static bool IsInBand2(this NpcAI npcAI) {
            return npcAI.SqrDistanceToOutOfCombatPoint <= SqrReturnToSpawnBand2;
        }

        public static bool IsOverBand1(this NpcAI npcAI) {
            return npcAI.SqrDistanceToOutOfCombatPoint > SqrReturnToSpawnBand1;
        }
        
        public static bool IsOverBand2(this NpcAI npcAI) {
            return npcAI.SqrDistanceToOutOfCombatPoint > SqrReturnToSpawnBand2;
        }
        
        public static bool IsOverBand3(this NpcAI npcAI) {
            return npcAI.SqrDistanceToOutOfCombatPoint > SqrReturnToSpawnBand3;
        }
        
        public static bool IsOverBand4(this NpcAI npcAI) {
            return npcAI.SqrDistanceToOutOfCombatPoint > SqrReturnToSpawnBand4;
        }

        public static int GetDistanceToLastIdlePointBand(this NpcAI npcAI) {
            var distance = npcAI.SqrDistanceToOutOfCombatPoint;
            return distance switch {
                <= SqrReturnToSpawnBand1 => 0,
                <= SqrReturnToSpawnBand2 => 1,
                _                        => 2,
            };
        }

        public static float MovementModifierByDistanceToLastIdlePoint(this NpcAI npcAI) {
            if (npcAI.InReturningToSpawn) {
                return Band2SpeedModifier;
            }
            return GetDistanceToLastIdlePointBand(npcAI) switch {
                1 => Band1SpeedModifier,
                2 => Band2SpeedModifier,
                _ => 1,
            };
        }

        public static float NewAlertModifierByDistanceToLastIdlePoint(this NpcAI npcAI) {
            return GetDistanceToLastIdlePointBand(npcAI) switch {
                1 => Band1NewAlertModifier,
                2 => Band2NewAlertModifier,
                _ => 1,
            };
        }

        public static float AlertDecreaseModifierByDistanceToLastIdlePoint(this NpcAI npcAI) {
            return GetDistanceToLastIdlePointBand(npcAI) switch {
                1 => Band1AlertDecreaseModifier,
                2 => Band2AlertDecreaseModifier,
                _ => 1,
            };
        }

        public static float MinHeroVisibilityByDistanceToLastIdlePoint(this NpcAI npcAI) {
            return GetDistanceToLastIdlePointBand(npcAI) switch {
                1 => Band1MinHeroVisibilityModifier,
                2 => Band2MinHeroVisibilityModifier,
                _ => 0,
            };
        }
        
        public static float CombatAggroDecreaseModifierByDistanceToLastIdlePoint(this NpcAI npcAI) {
            return GetDistanceToLastIdlePointBand(npcAI) switch {
                0 => Band0AggroDecreaseModifier,
                1 => Band1AggroDecreaseModifier,
                2 => Band2AggroDecreaseModifier,
                _ => 1,
            };
        }
        
        public static float LoseTargetDelayByDistanceToLastIdlePoint(this NpcAI npcAI) {
            return GetDistanceToLastIdlePointBand(npcAI) switch {
                0 => Band0LoseHeroDelay,
                1 => Band1LoseHeroDelay,
                2 => Band2LoseHeroDelay,
                _ => Band2LoseHeroDelay,
            };
        }
    }
}
