#if HDRP_10_0_0_OR_NEWER
#define USE_HYBRID_MOTION_PASS
#endif

using System;
using System.Collections.Generic;
using Unity.Assertions;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Entities.Graphics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.Rendering
{
    /// <summary>
    /// Represents how to setup and configure Entities Graphics entities.
    /// </summary>
    /// <remarks>
    /// This is useful to convert GameObjects into entities, or to set component values on entities directly.
    /// </remarks>
    public struct RenderMeshDescription
    {
        /// <summary>
        /// Filtering settings that determine when to draw the entity.
        /// </summary>
        public RenderFilterSettings FilterSettings;

        /// <summary>
        /// Determines what kinds of light probes the entity uses, if any.
        /// </summary>
        /// <remarks>
        /// This value corresponds to <see cref="LightProbeUsage"/>.
        /// </remarks>
        public LightProbeUsage LightProbeUsage;

        /// <summary>
        /// Construct a <see cref="RenderMeshDescription"/> using defaults from the given <see cref="Renderer"/> object.
        /// </summary>
        /// <param name="renderer">The renderer object (e.g. a <see cref="MeshRenderer"/>) to get default settings from.</param>
        public RenderMeshDescription(Renderer renderer)
        {
            Assert.IsTrue(renderer != null, "Must have a non-null Renderer to create RenderMeshDescription.");

            FilterSettings = new RenderFilterSettings
            {
                Layer = renderer.gameObject.layer,
                RenderingLayerMask = renderer.renderingLayerMask,
                ShadowCastingMode = renderer.shadowCastingMode,
                ReceiveShadows = renderer.receiveShadows,
                MotionMode = renderer.motionVectorGenerationMode,
                StaticShadowCaster = renderer.staticShadowCaster,
            };

            var staticLightingMode = RenderMeshUtility.StaticLightingModeFromRenderer(renderer);
            var lightProbeUsage = renderer.lightProbeUsage;

            LightProbeUsage = (staticLightingMode == RenderMeshUtility.StaticLightingMode.LightProbes)
                ? lightProbeUsage
                : LightProbeUsage.Off;
        }

        /// <summary>
        /// Construct a <see cref="RenderMeshDescription"/> using the given values.
        /// </summary>
        /// <param name="shadowCastingMode">Mode for shadow casting</param>
        /// <param name="receiveShadows">Mode for shadow receival</param>
        /// <param name="motionVectorGenerationMode">Mode for motion vectors generation</param>
        /// <param name="layer">Rendering layer</param>
        /// <param name="renderingLayerMask">Rendering layer mask</param>
        /// <param name="lightProbeUsage">Light probe usage mode</param>
        /// <param name="staticShadowCaster">Static shadow caster flag</param>
        public RenderMeshDescription(
            ShadowCastingMode shadowCastingMode,
            bool receiveShadows = false,
            MotionVectorGenerationMode motionVectorGenerationMode = MotionVectorGenerationMode.Camera,
            int layer = 0,
            uint renderingLayerMask = 0xffffffff,
            LightProbeUsage lightProbeUsage = LightProbeUsage.Off,
            bool staticShadowCaster = false)
        {
            FilterSettings = new RenderFilterSettings
            {
                Layer = layer,
                RenderingLayerMask = renderingLayerMask,
                ShadowCastingMode = shadowCastingMode,
                ReceiveShadows = receiveShadows,
                MotionMode = motionVectorGenerationMode,
                StaticShadowCaster = staticShadowCaster,
            };

            LightProbeUsage = lightProbeUsage;
        }
    }

    /// <summary>
    /// Helper class that contains static methods for populating entities
    /// so that they are compatible with the Entities Graphics package.
    /// </summary>
    public static class RenderMeshUtility
    {
        // Settings which affect what components an entity will get
        [Flags]
        internal enum EntitiesGraphicsComponentFlags
        {
            None = 0,
            GameObjectConversion = 1 << 0,
            InMotionPass = 1 << 1,
            LightProbesBlend = 1 << 2,
            LightProbesCustom = 1 << 3,
            DepthSorted = 1 << 4,
            Baking = 1 << 5,
            UseRenderMeshArray = 1 << 6
        }

        // Pre-generate ComponentTypes objects for each flag combination, so all the components
        // can be added at once, minimizing structural changes.
        internal class EntitiesGraphicsComponentTypes
        {
            private ComponentTypeSet[] m_ComponentTypePermutations;

            public EntitiesGraphicsComponentTypes()
            {
                // Subtract one because of "None"
                int numFlags = Enum.GetValues(typeof(EntitiesGraphicsComponentFlags)).Length - 1;

                var permutations = new List<ComponentTypeSet>();
                for (int flags = 0; flags < (1 << numFlags); ++flags)
                    permutations.Add(GenerateComponentTypes((EntitiesGraphicsComponentFlags)flags));

                m_ComponentTypePermutations = permutations.ToArray();
            }

            public ComponentTypeSet GetComponentTypes(EntitiesGraphicsComponentFlags flags) =>
                m_ComponentTypePermutations[(int) flags];

            public static ComponentTypeSet GenerateComponentTypes(EntitiesGraphicsComponentFlags flags)
            {
                List<ComponentType> components = new List<ComponentType>()
                {
                    // Absolute minimum set of components required by Entities Graphics
                    // to be considered for rendering. Entities without these components will
                    // not match queries and will never be rendered.
                    ComponentType.ReadWrite<WorldRenderBounds>(),
                    ComponentType.ReadWrite<RenderFilterSettings>(),
                    ComponentType.ReadWrite<MaterialMeshInfo>(),
                    ComponentType.ChunkComponent<ChunkWorldRenderBounds>(),
                    ComponentType.ChunkComponent<EntitiesGraphicsChunkInfo>(),
                    // Extra transform related components required to render correctly
                    // using many default SRP shaders. Custom shaders could potentially
                    // work without it.
                    ComponentType.ReadWrite<WorldToLocal_Tag>(),
                    // Components required by Entities Graphics package visibility culling.
                    ComponentType.ReadWrite<RenderBounds>(),
                    ComponentType.ReadWrite<PerInstanceCullingTag>(),
                };

                // RenderMesh is no longer used at runtime, it is only used during conversion.
                // At runtime all entities use RenderMeshArray.
                if (flags.HasFlag(EntitiesGraphicsComponentFlags.GameObjectConversion) | flags.HasFlag(EntitiesGraphicsComponentFlags.Baking) )
                    components.Add(ComponentType.ReadWrite<RenderMesh>());

                if (flags.HasFlag(EntitiesGraphicsComponentFlags.UseRenderMeshArray) && (!flags.HasFlag(EntitiesGraphicsComponentFlags.GameObjectConversion) | flags.HasFlag(EntitiesGraphicsComponentFlags.Baking) ))
                    components.Add(ComponentType.ReadWrite<RenderMeshArray>());

                // Baking uses TransformUsageFlags, and as such should not be explicitly adding LocalToWorld to anything
                if(!flags.HasFlag(EntitiesGraphicsComponentFlags.Baking))
                    components.Add(ComponentType.ReadWrite<LocalToWorld>());

                // Components required by objects that need to be rendered in per-object motion passes.
    #if USE_HYBRID_MOTION_PASS
                if (flags.HasFlag(EntitiesGraphicsComponentFlags.InMotionPass))
                    components.Add(ComponentType.ReadWrite<BuiltinMaterialPropertyUnity_MatrixPreviousM>());
    #endif

                if (flags.HasFlag(EntitiesGraphicsComponentFlags.LightProbesBlend))
                    components.Add(ComponentType.ReadWrite<BlendProbeTag>());
                else if (flags.HasFlag(EntitiesGraphicsComponentFlags.LightProbesCustom))
                    components.Add(ComponentType.ReadWrite<CustomProbeTag>());

                if (flags.HasFlag(EntitiesGraphicsComponentFlags.DepthSorted))
                    components.Add(ComponentType.ReadWrite<DepthSorted_Tag>());

                return new ComponentTypeSet(components.ToArray());
            }
        }

        internal static EntitiesGraphicsComponentTypes s_EntitiesGraphicsComponentTypes = new EntitiesGraphicsComponentTypes();

        // Use a boolean constant for guarding most of the code so both ifdef branches are
        // always compiled.
        // This leads to the following warning due to the other branch being unreachable, so disable it
        // warning CS0162: Unreachable code detected
#pragma warning disable CS0162

#if USE_HYBRID_MOTION_PASS
        internal const bool kUseHybridMotionPass = true;
#else
        internal const bool kUseHybridMotionPass = false;
#endif

        internal static ComponentTypeSet ComputeComponentTypes(EntitiesGraphicsComponentFlags flags, RenderMeshDescription renderMeshDescription, bool isStatic, List<Material> materials)
        {
            // Entities with Static are never rendered with motion vectors
            bool inMotionPass = kUseHybridMotionPass &&
                                renderMeshDescription.FilterSettings.IsInMotionPass &&
                                !isStatic;

            if (inMotionPass) flags |= EntitiesGraphicsComponentFlags.InMotionPass;
            flags |= LightProbeFlags(renderMeshDescription.LightProbeUsage);

            if (materials != null)
            {
                // Add DepthSorted flag bit for the whole entity if any submesh needs depth sorting.
                foreach (var material in materials)
                {
                    flags |= DepthSortedFlags(material);
                }
            }

            return s_EntitiesGraphicsComponentTypes.GetComponentTypes(flags);
        }

        /// <summary>
        /// Set the Entities Graphics component values to render the given entity using the given description.
        /// Any missing components will be added, which results in structural changes.
        /// </summary>
        /// <param name="entity">The entity to set the component values for.</param>
        /// <param name="entityManager">The <see cref="EntityManager"/> used to set the component values.</param>
        /// <param name="renderMeshDescription">The description that determines how the entity is to be rendered.</param>
        /// <param name="renderMeshArray">The instance of the RenderMeshArray which contains mesh and material.</param>
        /// <param name="materialMeshInfo">The MaterialMeshInfo used to index into renderMeshArray.</param>
        public static void AddComponents(
            Entity entity,
            EntityManager entityManager,
            in RenderMeshDescription renderMeshDescription,
            RenderMeshArray renderMeshArray,
            MaterialMeshInfo materialMeshInfo = default)
        {
            var materials = renderMeshArray.GetMaterials(materialMeshInfo);
            var mesh = renderMeshArray.GetMesh(materialMeshInfo);

            if (mesh == null)
            {
                Debug.LogError(("materialMeshInfo does not point to a valid mesh"));
                return;
            }
            if (materials == null)
            {
                Debug.LogError(("materialMeshInfo does not point to a valid material"));
                return;
            }

            // Add all components up front using as few calls as possible.
            var componentSet = ComputeComponentTypes(EntitiesGraphicsComponentFlags.UseRenderMeshArray,
                    renderMeshDescription, entityManager.HasComponent<Static>(entity), materials);
            entityManager.AddComponent(entity, componentSet);

            entityManager.SetSharedComponentManaged(entity, renderMeshDescription.FilterSettings);
            entityManager.SetSharedComponentManaged(entity, renderMeshArray);
            entityManager.SetComponentData(entity, materialMeshInfo);


            var localBounds = mesh.bounds.ToAABB();
            entityManager.SetComponentData(entity, new RenderBounds { Value = localBounds });
        }

        /// <summary>
        /// Set the Entities Graphics component values to render the given entity using the given description.
        /// Any missing components will be added, which results in structural changes.
        /// </summary>
        /// <param name="entity">The entity to set the component values for.</param>
        /// <param name="entityManager">The <see cref="EntityManager"/> used to set the component values.</param>
        /// <param name="renderMeshDescription">The description that determines how the entity is to be rendered.</param>
        /// <param name="materialMeshInfo">The MaterialMeshInfo containing the mesh and material ids </param>
        public static void AddComponents(
            Entity entity,
            EntityManager entityManager,
            in RenderMeshDescription renderMeshDescription,
            MaterialMeshInfo materialMeshInfo)
        {

            var egs = entityManager.World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
            if (egs == null)
            {
                Debug.LogError("Unable to get the Entities Graphics System");
                return;
            }

            var mesh = egs.GetMesh(materialMeshInfo.MeshID);
            var material = egs.GetMaterial(materialMeshInfo.MaterialID);
            if (mesh == null)
            {
                Debug.LogError(("materialMeshInfo does not point to a valid mesh"));
                return;
            }
            if (material == null)
            {
                Debug.LogError(("materialMeshInfo does not point to a valid material"));
                return;
            }

            // Add all components up front using as few calls as possible.
            var componentSet = ComputeComponentTypes(EntitiesGraphicsComponentFlags.None,
                renderMeshDescription, entityManager.HasComponent<Static>(entity), new List<Material> { material });
            entityManager.AddComponent(entity, componentSet);

            entityManager.SetSharedComponentManaged(entity, renderMeshDescription.FilterSettings);
            entityManager.SetComponentData(entity, materialMeshInfo);

            if (mesh == null)
                return;
            var localBounds = mesh.bounds.ToAABB();
            entityManager.SetComponentData(entity, new RenderBounds { Value = localBounds });
        }

#pragma warning restore CS0162
        internal static EntitiesGraphicsComponentFlags DepthSortedFlags(Material material)
        {
            if (IsMaterialTransparent(material))
                return EntitiesGraphicsComponentFlags.DepthSorted;
            else
                return EntitiesGraphicsComponentFlags.None;
        }

        private const string kSurfaceTypeHDRP = "_SurfaceType";
        private const string kSurfaceTypeURP = "_Surface";
        private static int kSurfaceTypeHDRPNameID = Shader.PropertyToID(kSurfaceTypeHDRP);
        private static int kSurfaceTypeURPNameID = Shader.PropertyToID(kSurfaceTypeURP);
        /// <summary>
        /// Return true if the given <see cref="Material"/> is known to be transparent. Works
        /// for materials that use HDRP or URP conventions for transparent materials.
        /// </summary>
        private static bool IsMaterialTransparent(Material material)
        {
            if (material == null)
                return false;

#if HDRP_10_0_0_OR_NEWER
            // Material.GetSurfaceType() is not public, so we try to do what it does internally.
            const int kSurfaceTypeTransparent = 1; // Corresponds to non-public SurfaceType.Transparent
            if (material.HasProperty(kSurfaceTypeHDRPNameID))
                return (int) material.GetFloat(kSurfaceTypeHDRPNameID) == kSurfaceTypeTransparent;
            else
                return false;
#elif URP_10_0_0_OR_NEWER
            const int kSurfaceTypeTransparent = 1; // Corresponds to SurfaceType.Transparent
            if (material.HasProperty(kSurfaceTypeURPNameID))
                return (int) material.GetFloat(kSurfaceTypeURPNameID) == kSurfaceTypeTransparent;
            else
                return false;
#else
            return false;
#endif
        }

        internal enum StaticLightingMode
        {
            None = 0,
            LightMapped = 1,
            LightProbes = 2,
        }

        internal static StaticLightingMode StaticLightingModeFromRenderer(Renderer renderer)
        {
            var staticLightingMode = StaticLightingMode.None;
            if (renderer.lightmapIndex >= 65534 || renderer.lightmapIndex < 0)
                staticLightingMode = StaticLightingMode.LightProbes;
            else if (renderer.lightmapIndex >= 0)
                staticLightingMode = StaticLightingMode.LightMapped;

            return staticLightingMode;
        }

        internal static EntitiesGraphicsComponentFlags LightProbeFlags(LightProbeUsage lightProbeUsage)
        {
            switch (lightProbeUsage)
            {
                case LightProbeUsage.BlendProbes:
                    return EntitiesGraphicsComponentFlags.LightProbesBlend;
                case LightProbeUsage.CustomProvided:
                    return EntitiesGraphicsComponentFlags.LightProbesCustom;
                default:
                    return EntitiesGraphicsComponentFlags.None;
            }
        }

        internal static string FormatRenderMesh(RenderMesh renderMesh) =>
            $"RenderMesh(material: {renderMesh.material}, mesh: {renderMesh.mesh}, subMesh: {renderMesh.subMesh})";

        internal static bool ValidateMesh(RenderMesh renderMesh)
        {
            if (renderMesh.mesh == null)
            {
                Debug.LogWarning($"RenderMesh must have a valid non-null Mesh. {FormatRenderMesh(renderMesh)}");
                return false;
            }
            else if (renderMesh.subMesh < 0 || renderMesh.subMesh >= renderMesh.mesh.subMeshCount)
            {
                Debug.LogWarning($"RenderMesh subMesh index out of bounds. {FormatRenderMesh(renderMesh)}");
                return false;
            }

            return true;
        }

        internal static bool ValidateMaterial(RenderMesh renderMesh)
        {
            if (renderMesh.material == null)
            {
                Debug.LogWarning($"RenderMesh must have a valid non-null Material. {FormatRenderMesh(renderMesh)}");
                return false;
            }

            return true;
        }

        internal static bool ValidateRenderMesh(RenderMesh renderMesh) =>
            ValidateMaterial(renderMesh) && ValidateMesh(renderMesh);

    }
}
