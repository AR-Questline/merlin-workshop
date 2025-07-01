// #define DEBUG_LOG_LIGHT_MAP_CONVERSION

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Hash128 = UnityEngine.Hash128;

namespace Unity.Rendering
{
    class LightMapBakingContext
    {
        [Flags]
        enum LightMappingFlags
        {
            None = 0,
            Lightmapped = 1,
            Directional = 2,
            ShadowMask = 4
        }

        struct MaterialLookupKey
        {
            public Material BaseMaterial;
            public LightMaps LightMaps;
            public LightMappingFlags Flags;
        }

        struct LightMapKey : IEquatable<LightMapKey>
        {
            public Hash128 ColorHash;
            public Hash128 DirectionHash;
            public Hash128 ShadowMaskHash;

            public LightMapKey(LightmapData lightmapData)
                : this(lightmapData.lightmapColor,
                    lightmapData.lightmapDir,
                    lightmapData.shadowMask)
            {
            }

            public LightMapKey(Texture2D color, Texture2D direction, Texture2D shadowMask)
            {
                ColorHash = default;
                DirectionHash = default;
                ShadowMaskHash = default;

#if UNITY_EDITOR
                // imageContentsHash only available in the editor, but this type is only used
                // during conversion, so it's only used in the editor.
                if (color != null) ColorHash = color.imageContentsHash;
                if (direction != null) DirectionHash = direction.imageContentsHash;
                if (shadowMask != null) ShadowMaskHash = shadowMask.imageContentsHash;
#endif
            }

            public bool Equals(LightMapKey other)
            {
                return ColorHash.Equals(other.ColorHash) && DirectionHash.Equals(other.DirectionHash) && ShadowMaskHash.Equals(other.ShadowMaskHash);
            }

            public override int GetHashCode()
            {
                var hash = new xxHash3.StreamingState(true);
                hash.Update(ColorHash);
                hash.Update(DirectionHash);
                hash.Update(ShadowMaskHash);
                return (int) hash.DigestHash64().x;
            }
        }

        public class LightMapReference
        {
            public LightMaps LightMaps;
            public int LightMapIndex;
        }

        private int m_NumLightMapCacheHits;
        private int m_NumLightMapCacheMisses;
        private int m_NumLightMappedMaterialCacheHits;
        private int m_NumLightMappedMaterialCacheMisses;

        private Dictionary<LightMapKey, LightMapReference> m_LightMapArrayCache;
        private Dictionary<MaterialLookupKey, Material> m_LightMappedMaterialCache = new Dictionary<MaterialLookupKey, Material>();

        private List<int> m_UsedLightmapIndices = new List<int>();
        private Dictionary<int, LightMapReference> m_LightMapReferences;

        public LightMapBakingContext()
        {
            Reset();
        }

        public void Reset()
        {
            m_LightMapArrayCache = new Dictionary<LightMapKey, LightMapReference>();
            m_LightMappedMaterialCache = new Dictionary<MaterialLookupKey, Material>();

            BeginConversion();
        }

        public void BeginConversion()
        {
            m_UsedLightmapIndices = new List<int>();
            m_LightMapReferences = new Dictionary<int, LightMapReference>();

            m_NumLightMapCacheHits = 0;
            m_NumLightMapCacheMisses = 0;
            m_NumLightMappedMaterialCacheHits = 0;
            m_NumLightMappedMaterialCacheMisses = 0;
        }

        public void EndConversion()
        {
#if DEBUG_LOG_LIGHT_MAP_CONVERSION
            Debug.Log($"Light map cache: {m_NumLightMapCacheHits} hits, {m_NumLightMapCacheMisses} misses. Light mapped material cache: {m_NumLightMappedMaterialCacheHits} hits, {m_NumLightMappedMaterialCacheMisses} misses.");
#endif
        }

        public void CollectLightMapUsage(Renderer renderer)
        {
            m_UsedLightmapIndices.Add(renderer.lightmapIndex);
        }

        // Check all light maps referenced within the current batch of converted Renderers.
        // Any references to light maps that have already been inserted into a LightMaps array
        // will be implemented by reusing the existing LightMaps object. Any leftover previously
        // unseen (or changed = content hash changed) light maps are combined into a new LightMaps array.
        public void ProcessLightMapsForConversion()
        {
            var lightmaps = LightmapSettings.lightmaps;
            var uniqueIndices = m_UsedLightmapIndices
                .Distinct()
                .OrderBy(x => x)
                .Where(x=> x >= 0 && x != 65534 && x < lightmaps.Length)
                .ToArray();

            var colors = new List<Texture2D>();
            var directions = new List<Texture2D>();
            var shadowMasks = new List<Texture2D>();
            var lightMapIndices = new List<int>();

            // Each light map reference is converted into a LightMapKey which identifies the light map
            // using the content hashes regardless of the index number. Previously encountered light maps
            // should be found from the cache even if their index number has changed. New or changed
            // light maps are placed into a new array.
            for (var i = 0; i < uniqueIndices.Length; i++)
            {
                var index = uniqueIndices[i];
                var lightmapData = lightmaps[index];
                var key = new LightMapKey(lightmapData);

                if (m_LightMapArrayCache.TryGetValue(key, out var lightMapRef))
                {
                    m_LightMapReferences[index] = lightMapRef;
                    ++m_NumLightMapCacheHits;
                }
                else
                {
                    colors.Add(lightmapData.lightmapColor);
                    directions.Add(lightmapData.lightmapDir);
                    shadowMasks.Add(lightmapData.shadowMask);
                    lightMapIndices.Add(index);
                    ++m_NumLightMapCacheMisses;
                }
            }

            if (lightMapIndices.Count > 0)
            {
#if DEBUG_LOG_LIGHT_MAP_CONVERSION
                Debug.Log($"Creating new DOTS light map array from {lightMapIndices.Count} light maps.");
#endif

                var lightMapArray = LightMaps.ConstructLightMaps(colors, directions, shadowMasks);

                for (int i = 0; i < lightMapIndices.Count; ++i)
                {
                    var lightMapRef = new LightMapReference
                    {
                        LightMaps = lightMapArray,
                        LightMapIndex = i,
                    };

                    m_LightMapReferences[lightMapIndices[i]] = lightMapRef;
                    m_LightMapArrayCache[new LightMapKey(colors[i], directions[i], shadowMasks[i])] = lightMapRef;
                }
            }
        }

        public LightMapReference GetLightMapReference(Renderer renderer)
        {
            if (m_LightMapReferences.TryGetValue(renderer.lightmapIndex, out var lightMapRef))
                return lightMapRef;
            else
                return null;
        }

        public Material GetLightMappedMaterial(Material baseMaterial, LightMapReference lightMapRef)
        {
            var flags = LightMappingFlags.Lightmapped;
            if (lightMapRef.LightMaps.hasDirections)
                flags |= LightMappingFlags.Directional;
            if (lightMapRef.LightMaps.hasShadowMask)
                flags |= LightMappingFlags.ShadowMask;

            var key = new MaterialLookupKey
            {
                BaseMaterial = baseMaterial,
                LightMaps = lightMapRef.LightMaps,
                Flags = flags
            };

            if (m_LightMappedMaterialCache.TryGetValue(key, out var lightMappedMaterial))
            {
                ++m_NumLightMappedMaterialCacheHits;
                return lightMappedMaterial;
            }
            else
            {
                ++m_NumLightMappedMaterialCacheMisses;
                lightMappedMaterial = CreateLightMappedMaterial(baseMaterial, lightMapRef.LightMaps);
                m_LightMappedMaterialCache[key] = lightMappedMaterial;
                return lightMappedMaterial;
            }
        }

        private static Material CreateLightMappedMaterial(Material material, LightMaps lightMaps)
        {
            var lightMappedMaterial = new Material(material);
            lightMappedMaterial.name = $"{lightMappedMaterial.name}_Lightmapped_";
            lightMappedMaterial.EnableKeyword("LIGHTMAP_ON");

            lightMappedMaterial.SetTexture("unity_Lightmaps", lightMaps.colors);
            lightMappedMaterial.SetTexture("unity_LightmapsInd", lightMaps.directions);
            lightMappedMaterial.SetTexture("unity_ShadowMasks", lightMaps.shadowMasks);

            if (lightMaps.hasDirections)
            {
                lightMappedMaterial.name = lightMappedMaterial.name + "_DIRLIGHTMAP";
                lightMappedMaterial.EnableKeyword("DIRLIGHTMAP_COMBINED");
            }

            if (lightMaps.hasShadowMask)
            {
                lightMappedMaterial.name = lightMappedMaterial.name + "_SHADOW_MASK";
            }

            return lightMappedMaterial;
        }
    }

    class RenderMeshBakingContext
    {
        private LightMapBakingContext m_LightMapBakingContext;

        /// <summary>
        /// Constructs a baking context that operates within a baking system.
        /// </summary>
        public RenderMeshBakingContext(
            LightMapBakingContext lightMapBakingContext = null)
        {
            m_LightMapBakingContext = lightMapBakingContext;
            m_LightMapBakingContext?.BeginConversion();
        }

        public void EndConversion()
        {
            m_LightMapBakingContext?.EndConversion();
        }

        public void CollectLightMapUsage(Renderer renderer)
        {
            Assert.IsTrue(m_LightMapBakingContext != null,
            "LightMapConversionContext must be set to call light mapping conversion methods.");
            m_LightMapBakingContext.CollectLightMapUsage(renderer);
        }

        public void ProcessLightMapsForConversion()
        {
            Assert.IsTrue(m_LightMapBakingContext != null,
            "LightMapConversionContext must be set to call light mapping conversion methods.");
            m_LightMapBakingContext.ProcessLightMapsForConversion();
        }

        internal Material ConfigureHybridLightMapping(
            Entity entity,
            EntityCommandBuffer ecb,
            Renderer renderer,
            Material material)
        {
            var staticLightingMode = RenderMeshUtility.StaticLightingModeFromRenderer(renderer);
            if (staticLightingMode == RenderMeshUtility.StaticLightingMode.LightMapped)
            {
                var lightMapRef = m_LightMapBakingContext.GetLightMapReference(renderer);

                if (lightMapRef != null)
                {
                    Material lightMappedMaterial =
                        m_LightMapBakingContext.GetLightMappedMaterial(material, lightMapRef);

                    ecb.AddComponent(entity,
                        new BuiltinMaterialPropertyUnity_LightmapST()
                            {Value = renderer.lightmapScaleOffset});
                    ecb.AddComponent(entity,
                        new BuiltinMaterialPropertyUnity_LightmapIndex() {Value = lightMapRef.LightMapIndex});
                    ecb.AddSharedComponentManaged(entity, lightMapRef.LightMaps);

                    return lightMappedMaterial;
                }
            }

            return null;
        }
    }
}
