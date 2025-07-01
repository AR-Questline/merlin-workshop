using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.Rendering
{
    /// <summary>
    /// Provides methods that help you to work with LOD groups.
    /// </summary>
    public static class LODGroupExtensions
    {
        /// <summary>
        /// Represents LOD parameters.
        /// </summary>
        public struct LODParams : IEqualityComparer<LODParams>, IEquatable<LODParams>
        {
            /// <summary>
            /// The LOD distance scale.
            /// </summary>
            public float  distanceScale;

            /// <summary>
            /// The camera position.
            /// </summary>
            public float3 cameraPos;

            /// <summary>
            /// Indicates whether the camera is in orthographic mode.
            /// </summary>
            public bool   isOrtho;

            /// <summary>
            /// The orthographic size of the camera.
            /// </summary>
            public float  orthosize;

            /// <inheritdoc/>
            public bool Equals(LODParams x, LODParams y)
            {
                return
                    x.distanceScale == y.distanceScale &&
                    x.cameraPos.Equals(y.cameraPos) &&
                    x.isOrtho == y.isOrtho &&
                    x.orthosize == y.orthosize;
            }

            /// <inheritdoc/>
            public bool Equals(LODParams x)
            {
                return
                    x.distanceScale == distanceScale &&
                    x.cameraPos.Equals(cameraPos) &&
                    x.isOrtho == isOrtho &&
                    x.orthosize == orthosize;
            }

            /// <inheritdoc/>
            public int GetHashCode(LODParams obj)
            {
                throw new System.NotImplementedException();
            }
        }

        public static float CalculateLodDistanceScale(float fieldOfView, float globalLodBias, bool isOrtho, float orthoSize)
        {
            float distanceScale;
            if (isOrtho)
            {
                distanceScale = 2.0f * orthoSize / globalLodBias;
            }
            else
            {
                var halfAngle = math.tan(math.radians(fieldOfView * 0.5F));
                // Half angle at 90 degrees is 1.0 (So we skip halfAngle / 1.0 calculation)
                distanceScale = (2.0f * halfAngle) / globalLodBias;
            }

            return distanceScale;
        }

        /// <summary>
        /// Calculates LOD parameters from an LODParameters object.
        /// </summary>
        /// <param name="parameters">The LOD parameters to use.</param>
        /// <param name="overrideLODBias">An optional LOD bias to apply.</param>
        /// <returns>Returns the calculated LOD parameters.</returns>
        public static LODParams CalculateLODParams(LODParameters parameters, float overrideLODBias = 0.0f)
        {
            LODParams lodParams;
            lodParams.cameraPos = parameters.cameraPosition;
            lodParams.isOrtho = parameters.isOrthographic;
            lodParams.orthosize = parameters.orthoSize;
            if (overrideLODBias == 0.0F)
                lodParams.distanceScale = CalculateLodDistanceScale(parameters.fieldOfView, QualitySettings.lodBias, lodParams.isOrtho, lodParams.orthosize);
            else
            {
                // overrideLODBias is not affected by FOV etc
                // This is useful if the FOV is continously changing (breaking LOD temporal cache) or you want to explicit control LOD bias.
                lodParams.distanceScale = 1.0F / overrideLODBias;
            }

            return lodParams;
        }

        /// <summary>
        /// Calculates LOD parameters from a camera.
        /// </summary>
        /// <param name="camera">The camera to calculate LOD parameters from.</param>
        /// <param name="overrideLODBias">An optional LOD bias to apply.</param>
        /// <returns>Returns the calculated LOD parameters.</returns>
        public static LODParams CalculateLODParams(Camera camera, float overrideLODBias = 0.0f)
        {
            LODParams lodParams;
            lodParams.cameraPos = camera.transform.position;
            lodParams.isOrtho = camera.orthographic;
            lodParams.orthosize = camera.orthographicSize;
            if (overrideLODBias == 0.0F)
                lodParams.distanceScale = CalculateLodDistanceScale(camera.fieldOfView, QualitySettings.lodBias, lodParams.isOrtho, lodParams.orthosize);
            else
            {
                // overrideLODBias is not affected by FOV etc.
                // This is useful if the FOV is continously changing (breaking LOD temporal cache) or you want to explicit control LOD bias.
                lodParams.distanceScale = 1.0F / overrideLODBias;
            }

            return lodParams;
        }

        /// <summary>
        /// Calculates the world size of an LOD group.
        /// </summary>
        /// <param name="lodGroup">The LOD group.</param>
        /// <returns>Returns the calculated world size of the LOD group.</returns>
        public static float GetWorldSpaceSize(LODGroup lodGroup)
        {
            return GetWorldSpaceScale(lodGroup.transform) * lodGroup.size;
        }

        internal static float GetWorldSpaceScale(Transform t)
        {
            var scale = t.lossyScale;
            float largestAxis = Mathf.Abs(scale.x);
            largestAxis = Mathf.Max(largestAxis, Mathf.Abs(scale.y));
            largestAxis = Mathf.Max(largestAxis, Mathf.Abs(scale.z));
            return largestAxis;
        }

        /// <summary>
        /// Calculates the current LOD index.
        /// </summary>
        /// <param name="lodDistances">The distances at which to switch between each LOD.</param>
        /// <param name="scale">The current LOD scale.</param>
        /// <param name="worldReferencePoint">A world-space reference point to base the LOD index calculation on.</param>
        /// <param name="lodParams">The LOD parameters to use.</param>
        /// <returns>Returns the calculated LOD index.</returns>
        public static int CalculateCurrentLODIndex(float4 lodDistances, float scale, float3 worldReferencePoint, ref LODParams lodParams)
        {
            var distanceSqr = CalculateDistanceSqr(worldReferencePoint, ref lodParams);
            var lodIndex = CalculateCurrentLODIndex(lodDistances * scale, distanceSqr);
            return lodIndex;
        }

        /// <summary>
        /// Calculates the current LOD mask.
        /// </summary>
        /// <param name="lodDistances">The distances at which to switch between each LOD.</param>
        /// <param name="scale">Current scale.</param>
        /// <param name="worldReferencePoint">A world-space reference point to base the LOD index calculation on.</param>
        /// <param name="lodParams">The LOD parameters to use.</param>
        /// <returns>Returns the calculated LOD mask.</returns>
        public static int CalculateCurrentLODMask(float4 lodDistances, float scale, float3 worldReferencePoint, ref LODParams lodParams)
        {
            var distanceSqr = CalculateDistanceSqr(worldReferencePoint, ref lodParams);
            return CalculateCurrentLODMask(lodDistances * scale, distanceSqr);
        }

        static int CalculateCurrentLODIndex(float4 lodDistances, float measuredDistanceSqr)
        {
            var lodResult = measuredDistanceSqr < (lodDistances * lodDistances);
            if (lodResult.x)
                return 0;
            else if (lodResult.y)
                return 1;
            else if (lodResult.z)
                return 2;
            else if (lodResult.w)
                return 3;
            else
                // Can return 0 or 16. Doesn't matter...
                return -1;
        }

        static int CalculateCurrentLODMask(float4 lodDistances, float measuredDistanceSqr)
        {
            var lodResult = measuredDistanceSqr < (lodDistances * lodDistances);
            if (lodResult.x)
                return 1;
            else if (lodResult.y)
                return 2;
            else if (lodResult.z)
                return 4;
            else if (lodResult.w)
                return 8;
            else
                // Can return 0 or 16. Doesn't matter...
                return 16;
        }

        static float CalculateDistanceSqr(float3 worldReferencePoint, ref LODParams lodParams)
        {
            if (lodParams.isOrtho)
            {
                return lodParams.distanceScale * lodParams.distanceScale;
            }
            else
            {
                return math.lengthsq(lodParams.cameraPos - worldReferencePoint) * (lodParams.distanceScale * lodParams.distanceScale);
            }
        }

        /// <summary>
        /// Calculates the world position of an LOD group.
        /// </summary>
        /// <param name="group">The LOD group.</param>
        /// <returns>Returns the world position of the LOD group.</returns>
        public static float3 GetWorldPosition(LODGroup group)
        {
            return group.GetComponent<Transform>().TransformPoint(group.localReferencePoint);
        }

        /// <summary>
        /// Calculates the LOD switch distance for an LOD group.
        /// </summary>
        /// <param name="fieldOfView">The field of view angle.</param>
        /// <param name="group">The LOD group.</param>
        /// <param name="lodIndex">The LOD index to use.</param>
        /// <returns>Returns the LOD switch distance.</returns>
        public static float CalculateLODSwitchDistance(float fieldOfView, LODGroup group, int lodIndex)
        {
            float halfAngle = math.tan(math.radians(fieldOfView) * 0.5F);
            return GetWorldSpaceSize(group) / (2 * group.GetLODs()[lodIndex].screenRelativeTransitionHeight * halfAngle);
        }
    }
}
