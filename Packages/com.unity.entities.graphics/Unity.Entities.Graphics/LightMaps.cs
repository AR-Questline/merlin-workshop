using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Unity.Rendering
{
    /// <summary>
    /// Represents a container for light maps.
    /// </summary>
    public struct LightMaps : ISharedComponentData, IEquatable<LightMaps>
    {
        /// <summary>
        /// An array of color maps.
        /// </summary>
        public Texture2DArray colors;

        /// <summary>
        /// An array of directional maps.
        /// </summary>
        public Texture2DArray directions;

        /// <summary>
        /// An array of Shadow masks.
        /// </summary>
        public Texture2DArray shadowMasks;

        /// <summary>
        /// Indicates whether the container stores any directional maps. 
        /// </summary>
        public bool hasDirections => directions != null && directions.depth > 0;

        /// <summary>
        /// Indicates whether the container stores any shadow masks.
        /// </summary>
        public bool hasShadowMask => shadowMasks != null && shadowMasks.depth > 0;

        /// <summary>
        /// Indicates whether the container stores any color maps.
        /// </summary>
        public bool isValid => colors != null;

        /// <inheritdoc/>
        public bool Equals(LightMaps other)
        {
            return
                colors == other.colors &&
                directions == other.directions &&
                shadowMasks == other.shadowMasks;
        }

        /// <summary>
        /// Calculates the hash code for this object.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            int hash = 0;
            if (!ReferenceEquals(colors, null)) hash ^= colors.GetHashCode();
            if (!ReferenceEquals(directions, null)) hash ^= directions.GetHashCode();
            if (!ReferenceEquals(shadowMasks, null)) hash ^= shadowMasks.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Converts a provided list of Texture2Ds into a Texture2DArray.
        /// </summary>
        /// <param name="source">A list of Texture2Ds.</param>
        /// <returns>Returns a Texture2DArray that contains the list of Texture2Ds.</returns>
        private static Texture2DArray CopyToTextureArray(List<Texture2D> source)
        {
            if (source == null || !source.Any())
                return null;

            var data = source.First();
            if (data == null)
                return null;

            bool isSRGB = GraphicsFormatUtility.IsSRGBFormat(data.graphicsFormat);
            var result = new Texture2DArray(data.width, data.height, source.Count, source[0].format, true, !isSRGB);
            result.filterMode = FilterMode.Trilinear;
            result.wrapMode = TextureWrapMode.Clamp;
            result.anisoLevel = 3;

            for (var sliceIndex = 0; sliceIndex < source.Count; sliceIndex++)
            {
                var lightMap = source[sliceIndex];
                Graphics.CopyTexture(lightMap, 0, result, sliceIndex);
            }

            return result;
        }

        /// <summary>
        /// Constructs a LightMaps instance from a list of textures for colors, direction lights, and shadow masks.
        /// </summary>
        /// <param name="inColors">The list of Texture2D for colors.</param>
        /// <param name="inDirections">The list of Texture2D for direction lights.</param>
        /// <param name="inShadowMasks">The list of Texture2D for shadow masks.</param>
        /// <returns>Returns a new LightMaps object.</returns>
        public static LightMaps ConstructLightMaps(List<Texture2D> inColors, List<Texture2D> inDirections, List<Texture2D> inShadowMasks)
        {
            var result = new LightMaps
            {
                colors = CopyToTextureArray(inColors),
                directions = CopyToTextureArray(inDirections),
                shadowMasks = CopyToTextureArray(inShadowMasks)
            };
            return result;
        }
    }
}
