using System.Collections.Generic;
using Unity.Assertions;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Rendering
{
    class MeshRendererBakingUtility
    {
        enum ConversionMode
        {
            Null,
            AttachToPrimaryEntity,
            AttachToPrimaryEntityForSingleMaterial,
            AttachToMultipleEntities
        }

        struct LODState
        {
            public LODGroup LodGroup;
            public Entity LodGroupEntity;
            public int LodGroupMask;
        }

        static void CreateLODState<T>(Baker<T> baker, Renderer authoringSource, out LODState lodState) where T : Component
        {
            // LODGroup
            lodState = new LODState();
            lodState.LodGroup = baker.GetComponentInParent<LODGroup>();
            lodState.LodGroupEntity = baker.GetEntity(lodState.LodGroup, TransformUsageFlags.Renderable);
            lodState.LodGroupMask = FindInLODs(lodState.LodGroup, authoringSource);
        }

        private static int FindInLODs(LODGroup lodGroup, Renderer authoring)
        {
            if (lodGroup != null)
            {
                var lodGroupLODs = lodGroup.GetLODs();

                int lodGroupMask = 0;

                // Find the renderer inside the LODGroup
                for (int i = 0; i < lodGroupLODs.Length; ++i)
                {
                    foreach (var renderer in lodGroupLODs[i].renderers)
                    {
                        if (renderer == authoring)
                        {
                            lodGroupMask |= (1 << i);
                        }
                    }
                }
                return lodGroupMask > 0 ? lodGroupMask : -1;
            }
            return -1;
        }

#pragma warning disable CS0162
        private static void AddRendererComponents<T>(Entity entity, Baker<T> baker, in RenderMeshDescription renderMeshDescription, RenderMesh renderMesh) where T : Component
        {
            // Add all components up front using as few calls as possible.
            var componentSet = RenderMeshUtility.ComputeComponentTypes(
                RenderMeshUtility.EntitiesGraphicsComponentFlags.Baking,
                renderMeshDescription, baker.IsStatic(), renderMesh.materials);
            baker.AddComponent(entity, componentSet);

            baker.SetSharedComponentManaged(entity, renderMesh);
            baker.SetSharedComponentManaged(entity, renderMeshDescription.FilterSettings);

            var localBounds = renderMesh.mesh.bounds.ToAABB();
            baker.SetComponent(entity, new RenderBounds { Value = localBounds });
        }

        internal static void ConvertToMultipleEntities<T>(Baker<T> baker,
            Renderer authoring,
            Mesh mesh,
            List<Material> sharedMaterials,
            Transform root,
            out List<Entity> additionalEntities) where T : Component
        {
            Convert(baker, authoring, mesh, sharedMaterials, ConversionMode.AttachToMultipleEntities, root, out additionalEntities);
        }

        internal static void ConvertOnPrimaryEntityForSingleMaterial<T>(Baker<T> baker,
            Renderer authoring,
            Mesh mesh,
            List<Material> sharedMaterials,
            Transform root,
            out List<Entity> additionalEntities) where T : Component
        {
            Convert(baker, authoring, mesh, sharedMaterials, ConversionMode.AttachToPrimaryEntityForSingleMaterial, root, out additionalEntities);
        }

        internal static void ConvertOnPrimaryEntity<T>(Baker<T> baker,
            Renderer authoring,
            Mesh mesh,
            List<Material> sharedMaterials) where T : Component
        {
            Convert(baker, authoring, mesh, sharedMaterials, ConversionMode.AttachToPrimaryEntity, null, out _);
        }

        private static void Convert<T>(Baker<T> baker,
            Renderer authoring,
            Mesh mesh,
            List<Material> sharedMaterials,
            ConversionMode conversionMode,
            Transform root,
            out List<Entity> additionalEntities) where T : Component
        {
            Assert.IsTrue(conversionMode != ConversionMode.Null);

            additionalEntities = new List<Entity>();

            if (mesh == null || sharedMaterials.Count == 0)
            {
                Debug.LogWarning(
                    $"Renderer is not converted because either the assigned mesh is null or no materials are assigned on GameObject {authoring.name}.",
                    authoring);
                return;
            }

            // Takes a dependency on the material
            foreach (var material in sharedMaterials)
                baker.DependsOn(material);

            // Takes a dependency on the mesh
            baker.DependsOn(mesh);

            // RenderMeshDescription accesses the GameObject layer.
            // Declaring the dependency on the GameObject with GetLayer, so the baker rebakes if the layer changes
            baker.GetLayer(authoring);
            var desc = new RenderMeshDescription(authoring);
            var renderMesh = new RenderMesh(authoring, mesh, sharedMaterials);

            // Always disable per-object motion vectors for static objects
            if (baker.IsStatic())
            {
                if (desc.FilterSettings.MotionMode == MotionVectorGenerationMode.Object)
                    desc.FilterSettings.MotionMode = MotionVectorGenerationMode.Camera;
            }

            bool attachToPrimaryEntity = false;
            attachToPrimaryEntity |= conversionMode == ConversionMode.AttachToPrimaryEntity;
            attachToPrimaryEntity |= conversionMode == ConversionMode.AttachToPrimaryEntityForSingleMaterial && sharedMaterials.Count == 1;

            if (attachToPrimaryEntity)
            {
                ConvertToSingleEntity(
                    baker,
                    desc,
                    renderMesh,
                    authoring);
            }
            else
            {
                ConvertToMultipleEntities(
                    baker,
                    desc,
                    renderMesh,
                    authoring,
                    sharedMaterials,
                    root,
                    out additionalEntities);
            }
        }

#pragma warning restore CS0162

        static void ConvertToSingleEntity<T>(
            Baker<T> baker,
            RenderMeshDescription renderMeshDescription,
            RenderMesh renderMesh,
            Renderer renderer) where T : Component
        {
            CreateLODState(baker, renderer, out var lodState);

            var entity = baker.GetEntity(renderer, TransformUsageFlags.Renderable);

            AddRendererComponents(entity, baker, renderMeshDescription, renderMesh);

            if (lodState.LodGroupEntity != Entity.Null && lodState.LodGroupMask != -1)
            {
                var lodComponent = new MeshLODComponent { Group = lodState.LodGroupEntity, LODMask = lodState.LodGroupMask };
                baker.AddComponent(entity, lodComponent);
            }
        }

        internal static void ConvertToMultipleEntities<T>(
            Baker<T> baker,
            RenderMeshDescription renderMeshDescription,
            RenderMesh renderMesh,
            Renderer renderer,
            List<Material> sharedMaterials,
            UnityEngine.Transform root,
            out List<Entity> additionalEntities) where T : Component
        {
            CreateLODState(baker, renderer, out var lodState);

            int materialCount = sharedMaterials.Count;
            additionalEntities = new List<Entity>();

            for (var m = 0; m != materialCount; m++)
            {
                Entity meshEntity;
                if (root == null)
                {
                    meshEntity = baker.CreateAdditionalEntity(TransformUsageFlags.Renderable, false, $"{baker.GetName()}-MeshRendererEntity");

                    // Update Transform components:
                    baker.AddComponent<AdditionalMeshRendererEntity>(meshEntity);
                }
                else
                {
                    meshEntity = baker.CreateAdditionalEntity(TransformUsageFlags.ManualOverride, false, $"{baker.GetName()}-MeshRendererEntity");

                    var localToWorld = root.localToWorldMatrix;
                    baker.AddComponent(meshEntity, new LocalToWorld {Value = localToWorld});

                    // TODO(DOTS-7063): FromMatrix should throw here if the matrix is unrepresentable as TransformData.
                    baker.AddComponent(meshEntity, LocalTransform.Identity);

                    if (!baker.IsStatic())
                    {
                        var rootEntity = baker.GetEntity(root, TransformUsageFlags.Renderable);
                        baker.AddComponent(meshEntity, new Parent {Value = rootEntity});
                    }
                }

                additionalEntities.Add(meshEntity);

                var material = sharedMaterials[m];

                renderMesh.subMesh  = m;
                renderMesh.material = material;

                AddRendererComponents(
                    meshEntity,
                    baker,
                    renderMeshDescription,
                    renderMesh);

                if (lodState.LodGroupEntity != Entity.Null && lodState.LodGroupMask != -1)
                {
                    var lodComponent = new MeshLODComponent { Group = lodState.LodGroupEntity, LODMask = lodState.LodGroupMask };
                    baker.AddComponent(meshEntity, lodComponent);
                }
            }
        }
    }
}
