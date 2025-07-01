using System;
using System.Collections.Generic;
using Awaken.TG.Main.Fights;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Utility.PhysicsUtils {
    /// <summary>
    /// https://answers.unity.com/questions/1678742/best-way-to-get-a-thickness-of-a-wall-between-2-po.html
    /// </summary>
    public static class CustomCastUtils {
        const int MaxWallsCount = 6;
        static readonly RaycastHit[] SourceHits = new RaycastHit[MaxWallsCount];
        static readonly RaycastHit[] DestinationHits = new RaycastHit[MaxWallsCount];
        static readonly RaycastHit[] SortedHits = new RaycastHit[MaxWallsCount + MaxWallsCount];
        
        /// Returns the width of each colliders between two points
        /// Does NOT work with a concave MeshCollider or TerrainColliders
        /// If A or B are inside a collider, they will be ignored unless includePartialColliders is true
        /// Width will be larger than the actual distance if colliders overlap
        /// <returns>True if there were less than 6 walls between source and destination</returns>
        public static bool TryDepthCast(Vector3 source, Vector3 destination, out float totalWidth, int layerMask = ~AIUtils.NotBlockingAIVisionAndAI) {
            totalWidth = 0f;

            Vector3 direction = destination - source;
            float distance = direction.magnitude;
            direction = direction.normalized;
            
            // Casting from both direction to get both front and back faces of the collider
            int sourceHitsCount = Physics.RaycastNonAlloc(source, direction, SourceHits, distance, layerMask);

            if (sourceHitsCount >= MaxWallsCount) {
                return false;
            }
            
            int destinationHitsCount = Physics.RaycastNonAlloc(destination, -direction, DestinationHits, distance, layerMask);
            
            if (destinationHitsCount >= MaxWallsCount) {
                return false;
            }
            
            // Sorting the result to group colliders and check that they do come in pairs
            Array.Copy(SourceHits, SortedHits, sourceHitsCount);
            Array.Copy(DestinationHits, 0, SortedHits, sourceHitsCount, destinationHitsCount);
            Array.Sort(SortedHits, 0, sourceHitsCount + destinationHitsCount, RaycastHitComparer.Instance);
            for (int i = 0; i < sourceHitsCount + destinationHitsCount;) {
                RaycastHit currentHit = SortedHits[i];
                if (i < SortedHits.Length - 1) {
                    RaycastHit nextHit = SortedHits[i + 1];
                    if (nextHit.collider == currentHit.collider) {
                        float width = distance - (nextHit.distance + currentHit.distance);
                        if (width < 0f) {
                            Log.Important?.Warning("Incorrect width for " + nextHit.collider.name + " of: " + width);
                        }
                        totalWidth += width;
                        if (i < SortedHits.Length - 2 && SortedHits[i + 2].collider == currentHit.collider) {
                            Log.Important?.Warning("Collider present more than twice for " + nextHit.collider.name);
                        }
                        i += 2;
                        continue;
                    }
                }
                i++;
            }

            ClearArrays();
            return true;
        }

        static void ClearArrays() {
            Array.Clear(DestinationHits, 0, DestinationHits.Length);
            Array.Clear(SourceHits, 0, SourceHits.Length);
            Array.Clear(SortedHits, 0, SortedHits.Length);
        }

        class RaycastHitComparer : IComparer<RaycastHit> {
            internal static readonly RaycastHitComparer Instance = new();
            public int Compare(RaycastHit hit1, RaycastHit hit2) {
                return hit1.collider.GetHashCode().CompareTo(hit2.collider.GetHashCode());
            }
        }
    }
}