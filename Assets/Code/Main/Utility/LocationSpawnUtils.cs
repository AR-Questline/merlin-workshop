using System;
using System.Linq;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.MVC;
using Pathfinding;
using UnityEngine;

namespace Awaken.TG.Main.Utility {
    public static class LocationSpawnUtils {
        const int AStarMeterCostMultiplier = 1000;

        [UnityEngine.Scripting.Preserve]
        public static void SpawnEnemiesAroundHero(int numberOfSpawns, Func<float> getRandomDistance, LocationTemplate enemyLocationToSpawn) {
            for (int i = 0; i < numberOfSpawns; i++) { 
                float spawnDistance = getRandomDistance();
                int theGScoreToStopAt = (int)(spawnDistance * AStarMeterCostMultiplier);
                RandomPath path = RandomPath.Construct(Hero.Current.Coords, theGScoreToStopAt, path => OnPathCalculated(path, enemyLocationToSpawn));
                AstarPath.StartPath(path);
            }
        }

        static void OnPathCalculated(Path path, LocationTemplate enemyLocationToSpawn) {
            // spawn enemy
            Location enemy = enemyLocationToSpawn.SpawnLocation((Vector3)path.path.Last().position, Quaternion.identity);
                
            // init enemy
            enemy.AfterFullyInitialized(() => {
                NpcElement npc = enemy.Element<NpcElement>();
                RepetitiveNpcUtils.Check(npc);
                npc.OnCompletelyInitialized(static npc => npc.NpcAI.EnterCombatWith(Hero.Current));
            });
        }
    }
}