using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Spawners.Critters {
    public static class CritterPools {
        public const int PoolCapacity = 100;
        public const int PoolCount = 2;
        [UnityEngine.Scripting.Preserve] public const int MaxCritterCount = PoolCapacity * PoolCount;
        static readonly Dictionary<ARAssetReference, PoolHandle> Pools = new();

        [UnityEngine.Scripting.Preserve]
        public static ArPrefabPool<Critter> GetPool(ARAssetReference critterPrefab) {
            if (Pools.TryGetValue(critterPrefab, out var poolHandle)) {
                poolHandle.count++;
                return poolHandle.pool;
            }

            Transform host = World.Services.Get<ViewHosting>().DefaultHost();
            var pool = new ArPrefabPool<Critter>(host, critterPrefab, GetCritter, ReleaseCritter, defaultCapacity: 10, maxSize: PoolCapacity);
            poolHandle = new PoolHandle(pool);
            Pools.Add(critterPrefab, poolHandle);
            return pool;

            static void ReleaseCritter(Critter critter) => critter.gameObject.SetActive(false);
            static void GetCritter(Critter critter) => critter.gameObject.SetActive(true);
        }

        [UnityEngine.Scripting.Preserve]
        public static void ReleasePool(ARAssetReference critterPrefabRef) {
            if (Pools.TryGetValue(critterPrefabRef, out var poolHandle)) {
                poolHandle.count--;
                if (poolHandle.count == 0) {
                    poolHandle.pool.Dispose();
                    Pools.Remove(critterPrefabRef);
                }
            }
        }
        
        public static void EDITOR_RuntimeReset() {
            foreach (var pool in Pools.Values) {
                pool.pool.Dispose();
            }
            Pools.Clear();
        }

        class PoolHandle {
            public readonly ArPrefabPool<Critter> pool;
            public int count;

            public PoolHandle(ArPrefabPool<Critter> pool) {
                this.pool = pool;
                count = 1;
            }
        }
    }
}