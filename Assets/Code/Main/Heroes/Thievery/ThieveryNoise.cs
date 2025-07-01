using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Maths;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Thievery {
    public static class ThieveryNoise {
        public static float strengthReactBoundary = 0.08f;
        
        public static void MakeNoise(float noiseRange, float noiseStrength, bool ignoreWalls, Vector3 noisePosition, IWithFaction source) {
            foreach (var npc in World.Services.Get<NpcGrid>().GetHearingNpcs(noisePosition, noiseRange)) {
                if (npc == source || !npc.IsFriendlyTo(source)) {
                    continue;
                }
                MakeNoise(npc.NpcAI, noiseRange, noiseStrength, ignoreWalls, noisePosition);
            }
        }

        static void MakeNoise(NpcAI npc, float noiseRange, float noiseStrength, bool ignoreWalls, Vector3 noisePosition) {
            if (AINoises.BlockedByWalls(npc, ignoreWalls, noisePosition, out float wallThickness)) {
                return;
            }
            
            var resultantNoise = noiseStrength;
            // scale strength by distance of npc relative to sound range
            float distance = npc.NpcElement.Coords.DistanceTo(noisePosition);
            var noiseFade = distance.RemapTo01(noiseRange * 0.9f, math.min(npc.Data.perception.MaxHearingRange, 2 * noiseRange), true);
            resultantNoise *= 1 - Mathf.Pow(noiseFade, 2);

            resultantNoise *= wallThickness.RemapTo01(AINoises.MaxWallThickness, 0);

            if (resultantNoise == 0) {
                return;
            }
            
            //Log.PC?.Debug?.Info($"Noise: {resultantNoise} at distance: {distance} with fade: {noiseFade} to {npc.NpcElement.Template.name}");
                
            if (resultantNoise < strengthReactBoundary) {
                return;
            }
            npc.NpcElement.Element<NpcCrimeReactions>().ReactToNoise(noisePosition, noiseRange, resultantNoise);
        }
    }
}