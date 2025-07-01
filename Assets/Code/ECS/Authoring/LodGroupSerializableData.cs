using System;
using Awaken.ECS.Utils;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Maths;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Awaken.ECS.Authoring {
    [Serializable]
    public struct LodGroupSerializableData {
        public BlittableBool isValid;
        public Vector3 localReferencePoint;
        public float4 lodDistances0;
        public float4 lodDistances1;
        public float4x4 localToWorldMatrix;

        public void Initialize(LODGroup lodGroup) {
            var transform = lodGroup.transform;
            var worldSize = LodUtils.GetWorldSpaceScale(transform) * lodGroup.size;
            localReferencePoint = lodGroup.localReferencePoint;
                
            lodDistances0 = new float4(float.PositiveInfinity);
            lodDistances1 = new float4(float.PositiveInfinity);
            var lods = lodGroup.GetLODs();
            for (var i = 0; i < lods.Length; ++i) {
                var d = worldSize / lods[i].screenRelativeTransitionHeight;
                if (i < 4) {
                    lodDistances0[i] = d;
                } else {
                    lodDistances1[i - 4] = d;
                }
            }
            localToWorldMatrix = transform.localToWorldMatrix;
            isValid = true;
        }

        public readonly LodGroupSerializableData WithLocalToWorldMatrix(float4x4 overrideLocalToWorldMatrix) {
            var previousScale = LodUtils.GetWorldSpaceScale(localToWorldMatrix);
            var newScale = LodUtils.GetWorldSpaceScale(overrideLocalToWorldMatrix);
            var factor = newScale / previousScale;
            return new() {
                isValid = isValid,
                localReferencePoint = localReferencePoint,
                lodDistances0 = lodDistances0 * factor,
                lodDistances1 = lodDistances1 * factor,
                localToWorldMatrix = overrideLocalToWorldMatrix,
            };
        }

        public readonly MeshLODGroupComponent ToLodGroupComponent() {
            return new() {
                LocalReferencePoint = localReferencePoint,
                LODDistances0 = lodDistances0,
                LODDistances1 = lodDistances1,
            };
        }
        
        public readonly LocalToWorld ToTransformComponent() {
            return new() {
                Value = localToWorldMatrix,
            };
        }

        public readonly LODWorldReferencePoint ToWorldReferencePoint() {
            return new LODWorldReferencePoint {
                Value = math.transform(localToWorldMatrix, localReferencePoint)
            };
        }
        
        public readonly LODRange CreateLODRange(int lodMask) {
            float minDist = float.MaxValue;
            float maxDist = 0.0F;

            if ((lodMask & 1 << 0) == 1 << 0) {
                minDist = 0.0f;
                maxDist = math.max(maxDist, lodDistances0.x);
            }
            if ((lodMask & 1 << 1) == 1 << 1) {
                minDist = math.min(minDist, lodDistances0.x);
                maxDist = math.max(maxDist, lodDistances0.y);
            }
            if ((lodMask & 1 << 2) == 1 << 2) {
                minDist = math.min(minDist, lodDistances0.y);
                maxDist = math.max(maxDist, lodDistances0.z);
            }
            if ((lodMask & 1 << 3) == 1 << 3) {
                minDist = math.min(minDist, lodDistances0.z);
                maxDist = math.max(maxDist, lodDistances0.w);
            }
            if ((lodMask & 1 << 4) == 1 << 4) {
                minDist = math.min(minDist, lodDistances0.w);
                maxDist = math.max(maxDist, lodDistances1.x);
            }
            if ((lodMask & 1 << 5) == 1 << 5) {
                minDist = math.min(minDist, lodDistances1.x);
                maxDist = math.max(maxDist, lodDistances1.y);
            }
            if ((lodMask & 1 << 6) == 1 << 6) {
                minDist = math.min(minDist, lodDistances1.y);
                maxDist = math.max(maxDist, lodDistances1.z);
            }
            if ((lodMask & 1 << 7) == 1 << 7) {
                minDist = math.min(minDist, lodDistances1.z);
                maxDist = math.max(maxDist, lodDistances1.w);
            }

            return new LODRange() {
                LODMask = lodMask,
                MinDist = minDist,
                MaxDist = maxDist,
            };
        }
        
        public readonly float GetMaxRenderingDistance() {
            float maxDist = 0.0F;
            maxDist = math.max(maxDist, math.select(lodDistances0.x, maxDist, float.IsInfinity(lodDistances0.x)));
            maxDist = math.max(maxDist, math.select(lodDistances0.y, maxDist, float.IsInfinity(lodDistances0.y)));
            maxDist = math.max(maxDist, math.select(lodDistances0.z, maxDist, float.IsInfinity(lodDistances0.z)));
            maxDist = math.max(maxDist, math.select(lodDistances0.w, maxDist, float.IsInfinity(lodDistances0.w)));
            maxDist = math.max(maxDist, math.select(lodDistances1.x, maxDist, float.IsInfinity(lodDistances1.x)));
            maxDist = math.max(maxDist, math.select(lodDistances1.y, maxDist, float.IsInfinity(lodDistances1.y)));
            maxDist = math.max(maxDist, math.select(lodDistances1.z, maxDist, float.IsInfinity(lodDistances1.z)));
            maxDist = math.max(maxDist, math.select(lodDistances1.w, maxDist, float.IsInfinity(lodDistances1.w)));
            return maxDist;
        }

        public float GetLODDistance(int lodIndex) {
            switch (lodIndex) {
                case 0:
                    return lodDistances0.x;
                case 1:
                    return lodDistances0.y;
                case 2:
                    return lodDistances0.z;
                case 3:
                    return lodDistances0.w;
                case 4:
                    return lodDistances1.x;
                case 5:
                    return lodDistances1.y;
                case 6:
                    return lodDistances1.z;
                case 7:
                    return lodDistances1.w;
            }
            Log.Important?.Error($"Not valid lodIndex {lodIndex}. Returning -1");
            return -1;
        }

        public readonly int LastValidLodIndex() {
            for (int i = 7; i >= 0; i--) {
                var lodDistance = i < 4 ? lodDistances0[i] : lodDistances1[i - 4];
                if (math.isfinite(lodDistance)) {
                    return i;
                }
            }
            return -1;
        }
    }
}
