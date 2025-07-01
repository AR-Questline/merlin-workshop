using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.Debugging;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Utility.PhysicsUtils;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Fights {
    public static class AINoises {
        public const float HearingStrengthPower = 2f;
        public const float MaxWallThickness = 0.5f;

        public static Vector3 GetPosition(ICharacter character, IGrounded fallback) {
            var head = character?.Head;
            return head != null ? head.position : fallback.Coords;
        }
        
        public static Vector3 GetPosition(ICharacter character) {
            return GetPosition(character, character);
        }
        
        public static void MakeNoise(float noiseRange, float noiseStrength, bool ignoreWalls, Vector3 noisePosition, params IWithFaction[] source) {
            if (source.IsNullOrEmpty()) {
                return;
            }
            
            var hearingNpcs = World.Services.Get<NpcGrid>().GetHearingNpcs(noisePosition, noiseRange);
            foreach (var npc in hearingNpcs) {
                if (source.Any(npc.IsHostileTo)) {
                    MakeNoiseInternal(noiseRange, noiseStrength, 1f, ignoreWalls, noisePosition, npc.NpcAI);
                }
            }
            DebugNoise(noiseRange, noisePosition, noiseStrength);
        }

        public static void MakeNoise(float noiseRange, float noiseStrength, bool ignoreWalls, Vector3 noisePosition, IWithFaction source) {
            var hearingNpcs = World.Services.Get<NpcGrid>().GetHearingNpcs(noisePosition, noiseRange);
            foreach (var npc in hearingNpcs) {
                if (npc.IsHostileTo(source)) {
                    MakeNoiseInternal(noiseRange, noiseStrength, 1f, ignoreWalls, noisePosition, npc.NpcAI);
                }
            }
            DebugNoise(noiseRange, noisePosition, noiseStrength);
        }
        
        public static void MakeNoiseOverTime(float noiseRange, float noiseStrength, float deltaTime, bool ignoreWalls, Vector3 noisePosition, IWithFaction source) {
            var hearingNpcs = World.Services.Get<NpcGrid>().GetHearingNpcs(noisePosition, noiseRange);
            foreach (var npc in hearingNpcs) {
                if (npc.IsHostileTo(source)) {
                    MakeNoiseInternal(noiseRange, noiseStrength, deltaTime, ignoreWalls, noisePosition, npc.NpcAI);
                }
            }
            DebugNoise(noiseRange, noisePosition, noiseStrength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MakeNoise(float noiseRange, float noiseStrength, bool ignoreWalls, Vector3 noisePosition, NpcAI possibleTarget, Vector3? poiPosition = null) {
            MakeNoiseInternal(noiseRange, noiseStrength, 1f, ignoreWalls, noisePosition, possibleTarget, poiPosition);
            DebugNoise(noiseRange, noisePosition, noiseStrength);
        }

        static void MakeNoiseInternal(float noiseRange, float noiseStrength, float endMultiplier, bool ignoreWalls, Vector3 noisePosition, NpcAI possibleTarget, Vector3? poiPosition = null) {
            poiPosition ??= noisePosition;
            bool isHearing = possibleTarget.TryCalculateAlertStrength(noiseRange, noiseStrength, endMultiplier, ignoreWalls, noisePosition, out float alertStrength, out bool knownPosition);
            if (isHearing) {
                if (knownPosition) {
                    possibleTarget.AlertStack.NewPoi(alertStrength, poiPosition.Value);
                } else {
                    possibleTarget.AlertStack.NewPoIWithHiddenPosition(alertStrength, poiPosition.Value);
                }
            }
        }

        public static bool TryCalculateAlertStrength(this NpcAI target, float range, float noiseStrength, float endMultiplier, bool ignoreWalls, Vector3 center, out float alertStrength, out bool knownPosition) {
            alertStrength = 0;
            knownPosition = false;
            float hearingStat = target.ParentModel.NpcStats.Hearing;
            Vector3 dirToTarget = target.NpcElement.Head.position - center;
            float upwardsDot = Mathf.Abs(Vector3.Dot(dirToTarget.normalized, Vector3.up));
            if (upwardsDot > 0.25f) {
                range *= 1 - (upwardsDot * 0.5f);
            }
            float distance = dirToTarget.magnitude;
            float minDistanceToHear = distance - range;
            float hearingRange = target.Data.perception.MaxHearingRange * hearingStat;

            if (hearingRange <= minDistanceToHear) {
                return false;
            }

            float coreHearingRange = target.Data.perception.CoreHearingRange * hearingStat;
            float x = minDistanceToHear.Remap(coreHearingRange, hearingRange, 1, 0, true);
            float hearStrength = Mathf.Pow(x, HearingStrengthPower) * noiseStrength;
            
            // In close range we can hear always
            bool isInCloseRange = distance <= coreHearingRange;
            if (isInCloseRange) {
                alertStrength = (int)AlertStack.AlertStrength.NoiseClose * hearStrength * endMultiplier;
                knownPosition = true;
                return true;
            }
            
            // Check if we can hear at all
            if (hearStrength < NoiseStrength.Inaudible) {
                return false;
            }
            
            // Check if sound is behind wall
            if (BlockedByWalls(target, ignoreWalls, center, out float wallThickness)) {
                return false;
            }
            
            alertStrength = Mathf.Lerp((int)AlertStack.AlertStrength.NoiseDistant, (int)AlertStack.AlertStrength.Min, wallThickness / MaxWallThickness);
            alertStrength *= hearStrength;
            
            knownPosition = alertStrength > (int)AlertStack.AlertStrength.NoiseKnownPosition;
            alertStrength *= endMultiplier;
            return true;
        }

        public static bool BlockedByWalls(NpcAI target, bool ignoreWalls, Vector3 center, out float wallThickness) {
            if (!ignoreWalls) {
                bool success = CustomCastUtils.TryDepthCast(target.VisionDetectionOrigin, center, out wallThickness);
                if (!success || wallThickness > MaxWallThickness) {
                    return true;
                }
            } else {
                bool canSee = AIUtils.CanSee(target.VisionDetectionOrigin, center, ~AIUtils.NotBlockingAIVisionAndAI);
                wallThickness = canSee ? 0 : MaxWallThickness / 2f;
            }

            return false;
        }

        [Conditional("DEBUG")]
        static void DebugNoise(float range, Vector3 position, float strength) {
            const float MediumNoiseStrength = 5f;
            if (!Perception.DebugHearingMarkers) {
                return;
            }
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Noise Strength Debug";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = Ground.SnapToGround(position) + Vector3.up * 0.1f;
            go.transform.localScale = new Vector3(range * 2, 0.1f, range * 2);
            Material material = go.GetComponent<MeshRenderer>().material;
            float strPerc = strength / MediumNoiseStrength;
            material.color = PerceptionDebug.GetColor(strPerc);
            Object.Destroy(go, 1f);
        }
    }
}