using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using QFSW.QC;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;
using Log = Awaken.Utility.Debugging.Log;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools {
    public static class QCDebugVFXTester {
#if AR_DEBUG || DEBUG
        static readonly Queue<GameObject> SpawnedVFXInstances = new();
        static readonly Queue<int> SpawnBatchSizes = new();

        [Command("vfx.spawn-on-ground", "Spawns VFX effects on the ground in front of the player with specified count and spread")]
        [UnityEngine.Scripting.Preserve]
        static void SpawnVFXOnGround([VFXName] string vfxAssetPath, int count = 1, float spread = 5f) {
            SpawnVFXInternal(vfxAssetPath, count, spread, false);
        }

        [Command("vfx.spawn-elevated", "Spawns VFX effects 20cm above player's feet in XZ plane with specified count and spread")]
        [UnityEngine.Scripting.Preserve]
        static void SpawnVFXElevated([VFXName] string vfxAssetPath, int count = 1, float spread = 5f) {
            SpawnVFXInternal(vfxAssetPath, count, spread, true);
        }

        [Command("vfx.despawn-last", "Despawns the last spawned VFX batch")]
        [UnityEngine.Scripting.Preserve]
        static void DespawnLastVFX() {
            if (SpawnedVFXInstances.Count == 0 || SpawnBatchSizes.Count == 0) {
                QuantumConsole.Instance.LogToConsoleAsync("No spawned VFX batches to despawn");
                return;
            }

            int lastBatchSize = SpawnBatchSizes.Dequeue();

            int actualDespawned = 0;
            for (int i = 0; i < lastBatchSize && SpawnedVFXInstances.Count > 0; i++) {
                var lastInstance = SpawnedVFXInstances.Dequeue();

                if (lastInstance) {
                    Object.DestroyImmediate(lastInstance);
                }
                actualDespawned++;
            }

            QuantumConsole.Instance.LogToConsoleAsync($"Despawned last batch of {actualDespawned} VFX instances. {SpawnedVFXInstances.Count} remaining");
        }

        [Command("vfx.despawn-all", "Despawns all spaw1ned VFX instances")]
        [UnityEngine.Scripting.Preserve]
        static void DespawnAllVFX() {
            int totalCount = SpawnedVFXInstances.Count;

            while (SpawnedVFXInstances.Count > 0) {
                var instance = SpawnedVFXInstances.Dequeue();
                if (instance) {
                    Object.DestroyImmediate(instance);
                }
            }

            SpawnBatchSizes.Clear();

            QuantumConsole.Instance.LogToConsoleAsync($"Despawned all {totalCount} VFX instances");
        }

        static void SpawnVFXInternal(string vfxName, int count, float spread, bool elevated) {
            var hero = Hero.Current;
            if (hero == null) {
                QuantumConsole.Instance.LogToConsoleAsync("Hero not found");
                return;
            }

            var vfxCollection = DebugVFXCollectionUtils.DEBUG_GetVfxCollectionWithWaitForCompletion();
            if (!vfxCollection) {
                QuantumConsole.Instance.LogToConsoleAsync("VfxCollection not found");
                return;
            }

            GameObject vfxPrefab = vfxCollection.customPrefabs?.FirstOrDefault(p => p && p.name == vfxName) ??
                                   vfxCollection.vfxPrefabs?.FirstOrDefault(p => p && p.name == vfxName);
            
            VisualEffectAsset vfxAsset = vfxPrefab ? null : vfxCollection.vfxAssets?.FirstOrDefault(a => a && a.name == vfxName);
            
            if (!vfxPrefab && !vfxAsset) {
                QuantumConsole.Instance.LogToConsoleAsync($"VFX '{vfxName}' not found in VfxCollection");
                return;
            }

            var heroPosition = hero.Coords;
            var heroForward = hero.ActorTransform.forward;

            SpawnVFXInstancesWithSpread(vfxPrefab, vfxAsset, heroPosition, heroForward, count, spread, !elevated);
            SpawnBatchSizes.Enqueue(count);

            QuantumConsole.Instance.LogToConsoleAsync($"Spawned {count} VFX instances at {(elevated ? "elevated" : "ground")} level with {spread}m spread");
        }

        static void SpawnVFXInstancesWithSpread(GameObject vfxPrefab, VisualEffectAsset vfxAsset, Vector3 basePosition, Vector3 forward, int count,
            float spread, bool shouldSnapToGround) {
            if (count == 1) {
                var spawnPosition = basePosition + forward * 2f;
                spawnPosition = GetVFXPosition(spawnPosition, shouldSnapToGround);
                SpawnSingleVFXInstance(vfxPrefab, vfxAsset, spawnPosition, forward);
                return;
            }

            int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
            int rows = Mathf.CeilToInt((float)count / cols);
            var right = Vector3.Cross(forward, Vector3.up).normalized;
            float spacing = spread / math.max(cols - 1, 1);
            float startX = -(cols - 1) * spacing * 0.5f;
            float startZ = 2f;

            int spawnedCount = 0;
            for (int row = 0; row < rows && spawnedCount < count; row++) {
                for (int col = 0; col < cols && spawnedCount < count; col++) {
                    float xOffset = startX + col * spacing;
                    float zOffset = startZ + row * spacing;
                    var spawnPosition = basePosition + right * xOffset + forward * zOffset;
                    spawnPosition = GetVFXPosition(spawnPosition, shouldSnapToGround);
                    SpawnSingleVFXInstance(vfxPrefab, vfxAsset, spawnPosition, forward);
                    spawnedCount++;
                }
            }
        }

        static void SpawnSingleVFXInstance(GameObject vfxPrefab, VisualEffectAsset vfxAsset, Vector3 position, Vector3 forward) {
            try {
                GameObject instance = null;

                if (vfxPrefab) {
                    instance = Object.Instantiate(vfxPrefab, position, Quaternion.LookRotation(forward));
                } else if (vfxAsset) {
                    instance = new GameObject($"VFX_{vfxAsset.name}");
                    instance.transform.position = position;
                    instance.transform.rotation = Quaternion.LookRotation(forward);

                    var visualEffect = instance.AddComponent<VisualEffect>();
                    visualEffect.visualEffectAsset = vfxAsset;
                    visualEffect.Play();
                }

                if (instance) {
                    SpawnedVFXInstances.Enqueue(instance);
                } else {
                    Log.Important?.Warning($"Failed to instantiate VFX at position {position}");
                }
            } catch (System.Exception ex) {
                Log.Critical?.Error($"Exception while spawning VFX: {ex.Message}");
                QuantumConsole.Instance.LogToConsoleAsync($"Failed to spawn VFX: {ex.Message}");
            }
        }
        
        static Vector3 GetVFXPosition(Vector3 position, bool shouldSnapToGround) {
            return shouldSnapToGround 
                ? Ground.SnapToGround(position + new Vector3(0f, 100, 0f)) 
                : position + new Vector3(0f, 0.2f, 0f);
        }
#endif
    }
}
