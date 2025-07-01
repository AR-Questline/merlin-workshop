using Awaken.Utility.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.Utility.Maths {
    [BurstCompile]
    public static class CullingUtils {
        [BurstCompile]
        public static void LightCullingSetup(in BatchCullingContext cullingContext,
            out ReceiverSphereCuller receiverSphereCuller, out NativeArray<float4> frustumPlanes,
            out NativeArray<int> frustumSplits, out NativeArray<float4> receivers,
            out NativeArray<float4> lightFacingFrustumPlanes) {
            var receiverPlanes = ReceiverPlanes.Create(cullingContext, ARAlloc.Temp);
            receiverSphereCuller = ReceiverSphereCuller.Create(cullingContext, ARAlloc.TempJob);
            (frustumPlanes, frustumSplits, receivers) = CreateFrustumPlanes(cullingContext, receiverPlanes.planes.AsArray(), receiverSphereCuller, ARAlloc.TempJob);
            lightFacingFrustumPlanes = receiverPlanes.CopyLightFacingFrustumPlanes(ARAlloc.TempJob);
            receiverPlanes.planes.Dispose();
        }

        // === SIMD
        public static void LightSimdCulling(
            NativeArray<float4> receiversPlanes, NativeArray<int> frustumSplits, NativeArray<float4> cullingPlanes,
            in float3x3 worldToLightSpaceRotation, NativeArray<SphereSplitInfo> spheresSplitInfos,
            NativeArray<float4> lightFacingFrustumPlanes,
            in float4 simdXs, in float4 simdYs, in float4 simdZs, in float4 simdRadii,
            out uint4 mask) {
            var splitMask = uint4.zero;
            for (int i = 0; i < frustumSplits.Length; i++) {
                splitMask |= 1u << i;
            }

            if (receiversPlanes.Length > 0) {
                var receiversMask = new bool4(true);
                for (int j = 0; j < receiversPlanes.Length; j++) {
                    var plane = receiversPlanes[j];
                    receiversMask &= plane.x * simdXs + plane.y * simdYs + plane.z * simdZs + plane.w + simdRadii > 0.0f;
                }
                splitMask = math.select(uint4.zero, splitMask, receiversMask);
            }

            if (math.any(splitMask != 0)) {
                LightSimdPlanesCulling(frustumSplits, cullingPlanes, simdXs, simdYs, simdZs, simdRadii, ref splitMask);
            }

            if (math.any(splitMask != 0) & (spheresSplitInfos.Length > 0)) {
                splitMask &= LightSpheresSimdCull(worldToLightSpaceRotation, spheresSplitInfos, lightFacingFrustumPlanes, simdXs, simdYs, simdZs, simdRadii);
            }

            mask = splitMask;
        }

        static void LightSimdPlanesCulling(NativeArray<int> frustumSplits, NativeArray<float4> cullingPlanes,
            in float4 simdXs, in float4 simdYs, in float4 simdZs, in float4 simdRadii, ref uint4 splitMask) {
            var frustumIndex = 0;
            var planesMask = uint4.zero;
            for (int i = 0; i < frustumSplits.Length; i++) {
                var frustumMask = new bool4(true);
                var splitsCount = frustumSplits[i];
                for (int j = 0; j < splitsCount; j++) {
                    var plane = cullingPlanes[frustumIndex++];
                    frustumMask &= plane.x * simdXs + plane.y * simdYs + plane.z * simdZs + plane.w + simdRadii > 0.0f;
                }
                planesMask |= math.select(uint4.zero, new uint4(1), frustumMask) << i;
            }
            splitMask &= planesMask;
        }

        static uint4 LightSpheresSimdCull(in float3x3 worldToLightSpaceRotation,
            NativeArray<SphereSplitInfo> spheresSplitInfos, NativeArray<float4> lightFacingFrustumPlanes,
            in float4 simdXs, in float4 simdYs, in float4 simdZs, in float4 simdRadii) {
            var spheresMask = uint4.zero;
            var coreContained = new bool4(false);

            var casterCenterLightSpaceXs = worldToLightSpaceRotation.c0.x * simdXs +
                                           worldToLightSpaceRotation.c1.x * simdYs +
                                           worldToLightSpaceRotation.c2.x * simdZs;
            var casterCenterLightSpaceYs = worldToLightSpaceRotation.c0.y * simdXs +
                                           worldToLightSpaceRotation.c1.y * simdYs +
                                           worldToLightSpaceRotation.c2.y * simdZs;
            var casterCenterLightSpaceZs = worldToLightSpaceRotation.c0.z * simdXs +
                                           worldToLightSpaceRotation.c1.z * simdYs +
                                           worldToLightSpaceRotation.c2.z * simdZs;

            // push the (light-facing) frustum planes back by the caster radius, then intersect with a line through the caster capsule center,
            // to compute the length of the shadow that will cover all possible receivers within the whole frustum (not just this split)
            var shadowDirection = math.transpose(worldToLightSpaceRotation).c2;
            var shadowLength = new float4(math.INFINITY);
            for (int j = 0; j < lightFacingFrustumPlanes.Length; ++j) {
                shadowLength = math.min(shadowLength, DistanceUntilCylinderFullyCrossesPlaneSimd(simdXs,
                    simdYs,
                    simdZs,
                    shadowDirection,
                    simdRadii,
                    lightFacingFrustumPlanes[j]));
            }
            shadowLength = math.max(shadowLength, 0.0f);

            var splitCount = spheresSplitInfos.Length;
            for (int splitIndex = 0; splitIndex < splitCount; ++splitIndex) {
                var splitInfo = spheresSplitInfos[splitIndex];
                var receiverCenterLightSpace = splitInfo.receiverSphereLightSpace.xyz;
                var receiverRadius = splitInfo.receiverSphereLightSpace.w;
                var receiverToCasterLightSpaceXs = casterCenterLightSpaceXs - receiverCenterLightSpace.x;
                var receiverToCasterLightSpaceYs = casterCenterLightSpaceYs - receiverCenterLightSpace.y;
                var receiverToCasterLightSpaceZs = casterCenterLightSpaceZs - receiverCenterLightSpace.z;

                // compute the light space z coordinate where the caster sphere and receiver sphere just intersect
                var sphereIntersectionMaxDistances = simdRadii + receiverRadius;
                var sphereIntersectionMaxDistancesSq =
                    sphereIntersectionMaxDistances * sphereIntersectionMaxDistances;
                var receiverToCasterLightSpaceDistancesSq =
                    receiverToCasterLightSpaceXs * receiverToCasterLightSpaceXs +
                    receiverToCasterLightSpaceYs * receiverToCasterLightSpaceYs;
                var zSqAtSphereIntersection = sphereIntersectionMaxDistancesSq -
                                              receiverToCasterLightSpaceDistancesSq;

                // if this is negative, the spheres do not overlap as circles in the XY plane, so cull the caster
                var check1 = zSqAtSphereIntersection < 0.0f;
                if (math.all(check1)) {
                    continue;
                }

                var distancesZSq = receiverToCasterLightSpaceZs * receiverToCasterLightSpaceZs;
                // if the caster is outside of the receiver sphere in the light direction, it cannot cast a shadow on it, so cull it
                var check2 = receiverToCasterLightSpaceZs > 0.0f & distancesZSq > zSqAtSphereIntersection;
                if (math.all(check2)) {
                    continue;
                }

                // render the caster in this split
                spheresMask |= math.select(new uint4(1), uint4.zero, check1 | check2) << splitIndex;

                // culling assumes that shaders will always sample from the cascade with the lowest index,
                // so if the caster capsule is fully contained within the "core" sphere where only this split index is sampled,
                // then cull this caster from all the larger index splits (break from this loop)
                // (it is sufficient to test that only the capsule start and end spheres are within the "core" receiver sphere)
                var coreRadius = receiverRadius * splitInfo.cascadeBlendCullingFactor;
                var receiverToShadowEndLightSpaceZs = receiverToCasterLightSpaceXs + shadowLength;
                var receiverToCasterLightSpaceLengthSq =
                    receiverToCasterLightSpaceXs * receiverToCasterLightSpaceXs +
                    receiverToCasterLightSpaceYs * receiverToCasterLightSpaceYs +
                    receiverToCasterLightSpaceZs * receiverToCasterLightSpaceZs;
                var receiverToShadowEndLightSpaceLengthSq =
                    receiverToCasterLightSpaceXs * receiverToCasterLightSpaceXs +
                    receiverToCasterLightSpaceXs * receiverToCasterLightSpaceXs +
                    receiverToShadowEndLightSpaceZs * receiverToShadowEndLightSpaceZs;
                var capsuleMaxDistance = coreRadius - simdRadii;
                var capsuleMaxDistanceSq = capsuleMaxDistance * capsuleMaxDistance;
                var capsuleDistanceSq = math.max(receiverToCasterLightSpaceLengthSq,
                    receiverToShadowEndLightSpaceLengthSq);

                var currentCoreMask = capsuleMaxDistance > 0.0f & capsuleDistanceSq < capsuleMaxDistanceSq;
                coreContained |= currentCoreMask;
                if (math.all(coreContained)) {
                    break;
                }
            }

            return spheresMask;
        }

        // === No scalar
        public static void LightCulling(
            NativeArray<float4> receiversPlanes, NativeArray<int> frustumSplits, NativeArray<float4> cullingPlanes,
            in float3x3 worldToLightSpaceRotation, NativeArray<SphereSplitInfo> spheresSplitInfos,
            NativeArray<float4> lightFacingFrustumPlanes,
            in float3 position, float r, out uint mask) {
            mask = 0;
            if (receiversPlanes.Length > 0) {
                var receiversMask = true;
                for (int j = 0; j < receiversPlanes.Length; j++) {
                    var plane = receiversPlanes[j];
                    receiversMask &= math.dot(plane.xyz, position) + plane.w + r > 0.0f;
                }
                if (!receiversMask) {
                    return;
                }
            }

            var frustumIndex = 0;
            for (int j = 0; j < frustumSplits.Length; j++) {
                var frustumMask = true;
                var splitsCount = frustumSplits[j];
                for (int k = 0; k < splitsCount; k++) {
                    var plane = cullingPlanes[frustumIndex++];
                    frustumMask &= math.dot(plane.xyz, position) + plane.w + r > 0.0f;
                }
                mask |= math.select(0u, 1u, frustumMask) << j;
            }

            if ((mask != 0) & (spheresSplitInfos.Length > 0)) {
                var spheresMask = 0u;
                var casterCenterLightSpace = math.mul(worldToLightSpaceRotation, position);

                // push the (light-facing) frustum planes back by the caster radius, then intersect with a line through the caster capsule center,
                // to compute the length of the shadow that will cover all possible receivers within the whole frustum (not just this split)
                var shadowDirection = math.transpose(worldToLightSpaceRotation).c2;
                var shadowLength = math.INFINITY;
                for (int j = 0; j < lightFacingFrustumPlanes.Length; ++j) {
                    shadowLength = math.min(shadowLength, DistanceUntilCylinderFullyCrossesPlane(position,
                        shadowDirection,
                        r,
                        lightFacingFrustumPlanes[j]));
                }
                shadowLength = math.max(shadowLength, 0.0f);

                var splitCount = spheresSplitInfos.Length;
                for (int splitIndex = 0; splitIndex < splitCount; ++splitIndex) {
                    var splitInfo = spheresSplitInfos[splitIndex];
                    var receiverCenterLightSpace = splitInfo.receiverSphereLightSpace.xyz;
                    var receiverRadius = splitInfo.receiverSphereLightSpace.w;
                    var receiverToCasterLightSpace = casterCenterLightSpace - receiverCenterLightSpace;

                    // compute the light space z coordinate where the caster sphere and receiver sphere just intersect
                    var sphereIntersectionMaxDistance = r + receiverRadius;
                    var zSqAtSphereIntersection = math.lengthsq(sphereIntersectionMaxDistance) -
                                                  math.lengthsq(receiverToCasterLightSpace.xy);

                    // if this is negative, the spheres do not overlap as circles in the XY plane, so cull the caster
                    if (zSqAtSphereIntersection < 0.0f) {
                        continue;
                    }

                    // if the caster is outside of the receiver sphere in the light direction, it cannot cast a shadow on it, so cull it
                    if (receiverToCasterLightSpace.z > 0.0f &&
                        math.lengthsq(receiverToCasterLightSpace.z) > zSqAtSphereIntersection) {
                        continue;
                    }

                    // render the caster in this split
                    spheresMask |= 1u << splitIndex;

                    // culling assumes that shaders will always sample from the cascade with the lowest index,
                    // so if the caster capsule is fully contained within the "core" sphere where only this split index is sampled,
                    // then cull this caster from all the larger index splits (break from this loop)
                    // (it is sufficient to test that only the capsule start and end spheres are within the "core" receiver sphere)
                    var coreRadius = receiverRadius * splitInfo.cascadeBlendCullingFactor;
                    var receiverToShadowEndLightSpace =
                        receiverToCasterLightSpace + new float3(0.0f, 0.0f, shadowLength);
                    var capsuleMaxDistance = coreRadius - r;
                    var capsuleDistanceSq = math.max(math.lengthsq(receiverToCasterLightSpace),
                        math.lengthsq(receiverToShadowEndLightSpace));
                    if (capsuleMaxDistance > 0.0f && capsuleDistanceSq < math.lengthsq(capsuleMaxDistance)) {
                        break;
                    }
                }
                mask &= spheresMask;
            }
        }

        static (NativeArray<float4> frustumPlanes, NativeArray<int> frustumSplits, NativeArray<float4> receivers)
            CreateFrustumPlanes(in BatchCullingContext cc, NativeArray<float4> receiverPlanes,
                in ReceiverSphereCuller receiverSphereCuller, Allocator allocator) {

            NativeArray<float4> receivers;
            if (!receiverSphereCuller.UseReceiverPlanes()) {
                receivers = new NativeArray<float4>(0, allocator, NativeArrayOptions.UninitializedMemory);
            } else {
                receivers = new NativeArray<float4>(receiverPlanes.Length, allocator, NativeArrayOptions.UninitializedMemory);
                for (var i = 0; i < receiverPlanes.Length; ++i) {
                    receivers[i] = receiverPlanes[i];
                }
            }

            var splitCount = cc.cullingSplits.Length;
            var totalPlanesCount = 0;
            for (int splitIndex = 0; splitIndex < splitCount; ++splitIndex) {
                totalPlanesCount += cc.cullingSplits[splitIndex].cullingPlaneCount;
            }

            var result = new NativeArray<float4>(totalPlanesCount, allocator, NativeArrayOptions.UninitializedMemory);
            var splits = new NativeArray<int>(splitCount, allocator, NativeArrayOptions.UninitializedMemory);
            var planesIndex = 0;
            for (int splitIndex = 0; splitIndex < splitCount; ++splitIndex) {
                CullingSplit split = cc.cullingSplits[splitIndex];
                splits[splitIndex] = split.cullingPlaneCount;

                // use all culling planes
                for (var i = 0; i < split.cullingPlaneCount; ++i) {
                    var plane = cc.cullingPlanes[split.cullingPlaneOffset + i];
                    result[planesIndex++] = new float4(plane.normal, plane.distance);
                }
            }

            return (result, splits, receivers);
        }

        static float4 DistanceUntilCylinderFullyCrossesPlaneSimd(
            float4 cylinderCenterXs,
            float4 cylinderCenterYs,
            float4 cylinderCenterZs,
            float3 cylinderDirection,
            float4 cylinderRadii,
            float4 plane) {
            float cosEpsilon = 0.001f; // clamp the cosine of glancing angles

            // compute the distance until the center intersects the plane
            var cosTheta = math.max(math.abs(math.dot(plane.xyz, cylinderDirection)), cosEpsilon);
            var heightsAbovePlane = cylinderCenterXs * plane.x + cylinderCenterYs * plane.y + cylinderCenterZs * plane.z + plane.w;
            var centerDistanceToPlane = heightsAbovePlane / cosTheta;

            // compute the additional distance until the edge of the cylinder intersects the plane
            var sinTheta = math.sqrt(math.max(1.0f - cosTheta * cosTheta, 0.0f));
            var edgeDistanceToPlane = cylinderRadii * sinTheta / cosTheta;

            return centerDistanceToPlane + edgeDistanceToPlane;
        }

        static float DistanceUntilCylinderFullyCrossesPlane(
            float3 cylinderCenter,
            float3 cylinderDirection,
            float cylinderRadius,
            float4 plane) {
            float cosEpsilon = 0.001f; // clamp the cosine of glancing angles

            // compute the distance until the center intersects the plane
            var cosTheta = math.max(math.abs(math.dot(plane.xyz, cylinderDirection)), cosEpsilon);
            var heightAbovePlane = math.dot(plane.xyz, cylinderCenter) + plane.w;
            var centerDistanceToPlane = heightAbovePlane / cosTheta;

            // compute the additional distance until the edge of the cylinder intersects the plane
            var sinTheta = math.sqrt(math.max(1.0f - cosTheta * cosTheta, 0.0f));
            var edgeDistanceToPlane = cylinderRadius * sinTheta / cosTheta;

            return centerDistanceToPlane + edgeDistanceToPlane;
        }
    }

    public struct ReceiverSphereCuller {
        public NativeArray<SphereSplitInfo> splitInfos;
        public float3x3 worldToLightSpaceRotation;

        public readonly bool UseReceiverPlanes() {
            // only use receiver planes if there are no receiver spheres
            // (if spheres are present, then this is directional light cascades and Unity has already added receiver planes to the culling planes)
            return splitInfos.Length == 0;
        }

        public static ReceiverSphereCuller Create(in BatchCullingContext cc, Allocator allocator) {
            int splitCount = cc.cullingSplits.Length;

            // only set up sphere culling when there are multiple splits and all splits have valid spheres
            var allSpheresValid = (splitCount > 1);
            for (int splitIndex = 0; splitIndex < splitCount; ++splitIndex) {
                // ensure that NaN is not considered as valid
                if (!(cc.cullingSplits[splitIndex].sphereRadius > 0.0f))
                    allSpheresValid = false;
            }
            if (!allSpheresValid) {
                splitCount = 0;
            }

            var lightToWorldSpaceRotation = (float3x3)(float4x4)cc.localToWorldMatrix;
            var result = new ReceiverSphereCuller {
                splitInfos =
                    new NativeArray<SphereSplitInfo>(splitCount, allocator, NativeArrayOptions.UninitializedMemory),
                worldToLightSpaceRotation = math.transpose(lightToWorldSpaceRotation),
            };

            for (int splitIndex = 0; splitIndex < splitCount; ++splitIndex) {
                var cullingSplit = cc.cullingSplits[splitIndex];

                var receiverSphereLightSpace = new float4(
                    math.mul(result.worldToLightSpaceRotation, cullingSplit.sphereCenter),
                    cullingSplit.sphereRadius);

                result.splitInfos[splitIndex] = new SphereSplitInfo() {
                    receiverSphereLightSpace = receiverSphereLightSpace,
                    cascadeBlendCullingFactor = cullingSplit.cascadeBlendCullingFactor,
                };
            }

            return result;
        }
    }

    public struct ReceiverPlanes {
        public NativeList<float4> planes;
        public int lightFacingPlaneCount;

        static bool IsSignBitSet(float x) {
            uint i = math.asuint(x);
            return (i >> 31) != 0;
        }

        internal NativeArray<float4> CopyLightFacingFrustumPlanes(Allocator allocator) {
            var facingPlanes = new NativeArray<float4>(lightFacingPlaneCount, allocator,
                NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < lightFacingPlaneCount; ++i) {
                facingPlanes[i] = planes[i];
            }
            return facingPlanes;
        }

        internal static ReceiverPlanes Create(in BatchCullingContext cc, Allocator allocator) {
            var result = new ReceiverPlanes {
                planes = new NativeList<float4>(allocator),
                lightFacingPlaneCount = 0,
            };

            if (cc.viewType == BatchCullingViewType.Light && cc.receiverPlaneCount != 0) {
                bool isLightOrthographic = false;
                if (cc.cullingSplits.Length > 0) {
                    var m = cc.cullingSplits[0].cullingMatrix;
                    isLightOrthographic = m[15] == 1.0f && m[11] == 0.0f && m[7] == 0.0f && m[3] == 0.0f;
                }
                if (isLightOrthographic) {
                    var lightDir = (float3)(Vector3)(-cc.localToWorldMatrix.GetColumn(2));

                    // cache result for each plane, add planes facing towards the light
                    int planeSignBits = 0;
                    for (int i = 0; i < cc.receiverPlaneCount; ++i) {
                        var plane = cc.cullingPlanes[cc.receiverPlaneOffset + i];
                        var facingTerm = math.dot(plane.normal, lightDir);
                        if (IsSignBitSet(facingTerm)) {
                            planeSignBits |= (1 << i);
                        } else {
                            result.planes.Add(new float4(plane.normal, plane.distance));
                        }
                    }
                    result.lightFacingPlaneCount = result.planes.Length;

                    // assume ordering +/-x, +/-y, +/-z for frustum planes, test pairs for silhouette edges
                    if (cc.receiverPlaneCount == 6) {
                        for (var i = 0; i < cc.receiverPlaneCount; ++i) {
                            for (var j = i + 1; j < cc.receiverPlaneCount; ++j) {
                                // skip pairs that are from the same frustum axis (i.e. both xs, both ys or both zs)
                                if ((i / 2) == (j / 2)) {
                                    continue;
                                }

                                // silhouette edges occur when the planes have opposing signs
                                int signCheck = ((planeSignBits >> i) ^ (planeSignBits >> j)) & 1;
                                if (signCheck == 0) {
                                    continue;
                                }

                                // process in consistent order for consistent plane normal in the result
                                var (indexA, indexB) = (((planeSignBits >> i) & 1) == 0) ? (i, j) : (j, i);
                                var planeA = cc.cullingPlanes[cc.receiverPlaneOffset + indexA];
                                var planeB = cc.cullingPlanes[cc.receiverPlaneOffset + indexB];

                                // construct a plane that contains the light origin and this silhouette edge
                                var planeEqA = new float4(planeA.normal, planeA.distance);
                                var planeEqB = new float4(planeB.normal, planeB.distance);
                                var silhouetteEdge = Line.LineOfPlaneIntersectingPlane(planeEqA, planeEqB);
                                var silhouettePlaneEq =
                                    Line.PlaneContainingLineWithNormalPerpendicularToVector(silhouetteEdge,
                                        lightDir);

                                // try to normalize
                                silhouettePlaneEq = silhouettePlaneEq / math.length(silhouettePlaneEq.xyz);
                                if (!math.any(math.isnan(silhouettePlaneEq))) {
                                    result.planes.Add(new float4(silhouettePlaneEq.xyz, silhouettePlaneEq.w));
                                }
                            }
                        }
                    }
                } else {
                    var lightPos = cc.localToWorldMatrix.GetPosition();

                    // cache result for each plane, add planes facing towards the light
                    var planeSignBits = 0;
                    for (int i = 0; i < cc.receiverPlaneCount; ++i) {
                        var plane = cc.cullingPlanes[cc.receiverPlaneOffset + i];
                        var distance = plane.GetDistanceToPoint(lightPos);
                        if (IsSignBitSet(distance)) {
                            planeSignBits |= (1 << i);
                        } else {
                            result.planes.Add(new float4(plane.normal, plane.distance));
                        }
                    }
                    result.lightFacingPlaneCount = result.planes.Length;

                    // assume ordering +/-x, +/-y, +/-z for frustum planes, test pairs for silhouette edges
                    if (cc.receiverPlaneCount == 6) {
                        for (int i = 0; i < cc.receiverPlaneCount; ++i) {
                            for (int j = i + 1; j < cc.receiverPlaneCount; ++j) {
                                // skip pairs that are from the same frustum axis (i.e. both xs, both ys or both zs)
                                if ((i / 2) == (j / 2)) {
                                    continue;
                                }

                                // silhouette edges occur when the planes have opposing signs
                                int signCheck = ((planeSignBits >> i) ^ (planeSignBits >> j)) & 1;
                                if (signCheck == 0) {
                                    continue;
                                }

                                // process in consistent order for consistent plane normal in the result
                                var (indexA, indexB) = (((planeSignBits >> i) & 1) == 0) ? (i, j) : (j, i);
                                var planeA = cc.cullingPlanes[cc.receiverPlaneOffset + indexA];
                                var planeB = cc.cullingPlanes[cc.receiverPlaneOffset + indexB];

                                // construct a plane that contains the light origin and this silhouette edge
                                var planeEqA = new float4(planeA.normal, planeA.distance);
                                var planeEqB = new float4(planeB.normal, planeB.distance);
                                var silhouetteEdge = Line.LineOfPlaneIntersectingPlane(planeEqA, planeEqB);
                                var silhouettePlaneEq = Line.PlaneContainingLineAndPoint(silhouetteEdge, lightPos);

                                // try to normalize
                                silhouettePlaneEq = silhouettePlaneEq / math.length(silhouettePlaneEq.xyz);
                                if (!math.any(math.isnan(silhouettePlaneEq))) {
                                    result.planes.Add(new float4(silhouettePlaneEq.xyz, silhouettePlaneEq.w));
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
    }

    public struct SphereSplitInfo {
        public float4 receiverSphereLightSpace;
        public float cascadeBlendCullingFactor;
    }

    // 6-component representation of a (infinite length) line in 3D space
    struct Line {
        // for the line to be valid, dot(m, t) == 0
        public float3 m;
        public float3 t;

        internal static Line LineOfPlaneIntersectingPlane(float4 a, float4 b) {
            // planes do not need to have a unit length normal
            return new Line {
                m = a.w * b.xyz - b.w * a.xyz,
                t = math.cross(a.xyz, b.xyz),
            };
        }

        internal static float4 PlaneContainingLineAndPoint(Line a, float3 b) {
            // the resulting plane will not have a unit length normal (and the normal will be approximately zero when no plane exists)
            return new float4(a.m + math.cross(a.t, b), -math.dot(a.m, b));
        }

        internal static float4 PlaneContainingLineWithNormalPerpendicularToVector(Line a, float3 b) {
            // the resulting plane will not have a unit length normal (and the normal will be approximately zero when no plane exists)
            return new float4(math.cross(a.t, b), -math.dot(a.m, b));
        }
    }
}
