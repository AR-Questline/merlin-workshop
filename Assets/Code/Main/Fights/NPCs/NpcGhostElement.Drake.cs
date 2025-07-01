using Awaken.Utility;
using System.Collections.Generic;
using Awaken.ECS.Authoring.LinkedEntities;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Awaken.ECS.DrakeRenderer.Components.MaterialOverrideComponents;
using Awaken.TG.Main.Utility.VFX;
using Unity.Entities;
using UnityEngine;

namespace Awaken.TG.Main.Fights.NPCs {
    /// <summary>
    /// Turns NPC into ghost, Drake implementation
    /// </summary>
#pragma warning disable AR0026 // TypeForSerialization
    public partial class NpcGhostElement {
#pragma warning restore AR0026

        List<LinkedEntitiesAccess> _linkedEntitiesAccesses = new();
        
        void ConvertRenderersToGhost(IEnumerable<LinkedEntitiesAccess> linkedEntitiesAccesses) {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                return;
            }
            var entityManager = world.EntityManager;
            
            foreach (var linkedEntitiesAccess in linkedEntitiesAccesses) {
                DissolveAbleRenderer dar = linkedEntitiesAccess.gameObject.GetComponent<DissolveAbleRenderer>();
                dar ??= linkedEntitiesAccess.gameObject.AddComponent<DissolveAbleRenderer>();
                dar.Init();
                
                var component = new TransitionOverrideComponent() {
                    value = _instant ? 1 : 0
                };

                Material[] materials;
                if (dar.hasCustomGhostMaterials) {
                    materials = GetCustomMaterials(linkedEntitiesAccess, entityManager, component, dar);
                } else {
                    materials = CreateNewMaterials(linkedEntitiesAccess, entityManager, component);
                }

                // Swap materials
                dar!.SetCustomDissolveAbleMaterials(materials, null, true);
                dar!.ChangeToDissolveAble();
                
                _linkedEntitiesAccesses.Add(linkedEntitiesAccess);
            }
        }

        Material[] GetCustomMaterials(LinkedEntitiesAccess linkedEntitiesAccess, EntityManager entityManager, TransitionOverrideComponent component, DissolveAbleRenderer dar) {
            foreach (var entity in linkedEntitiesAccess.LinkedEntities) {
                if (!entityManager.HasComponent<DrakeMeshMaterialComponent>(entity)) {
                    continue;
                }
                
                if (entityManager.HasComponent<TransitionOverrideComponent>(entity)) {
                    entityManager.SetComponentData(entity, component);
                } else {
                    entityManager.AddComponentData(entity, component);
                }
            }

            return dar.dissolveAbleMaterials;
        }

        Material[] CreateNewMaterials(LinkedEntitiesAccess linkedEntitiesAccess, EntityManager entityManager, TransitionOverrideComponent component) {
            List<Material> ghostMaterials = new();
            var manager = DrakeRendererManager.Instance;
            var loadingManager = manager.LoadingManager;
            foreach (var entity in linkedEntitiesAccess.LinkedEntities) {
                if (!entityManager.HasComponent<DrakeMeshMaterialComponent>(entity)) {
                    continue;
                }
                var meshMaterial = entityManager.GetComponentData<DrakeMeshMaterialComponent>(entity);
                loadingManager.TryGetLoadedMaterial(meshMaterial.materialIndex, out var runtimeKey, out var material);
                    
                var ghostMaterial = CreateNewMaterial(material);
                ghostMaterials.Add(ghostMaterial);
                    
                if (entityManager.HasComponent<TransitionOverrideComponent>(entity)) {
                    entityManager.SetComponentData(entity, component);
                } else {
                    entityManager.AddComponentData(entity, component);
                }
            }
            
            return ghostMaterials.ToArray();
        }

        void RemoveGhostRenderers(IEnumerable<LinkedEntitiesAccess> linkedEntitiesAccesses) {
            foreach (var linkedEntitiesAccess in linkedEntitiesAccesses) {
                _linkedEntitiesAccesses.Remove(linkedEntitiesAccess);
            }
        }

        void UpdateDrakeMaterials(float percent) {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                return;
            }
            var entityManager = world.EntityManager;
            var component = new TransitionOverrideComponent() {
                value = percent
            };
            foreach (var linkedEntitiesAccess in _linkedEntitiesAccesses) {
                foreach (var entity in linkedEntitiesAccess.LinkedEntities) {
                    if (!entityManager.HasComponent<DrakeMeshMaterialComponent>(entity)) {
                        continue;
                    }
                    entityManager.SetComponentData(entity, component);
                }
            }
        }

        void FinishDrakeRevertChanges() {
            UpdateDrakeMaterials(0);
        }
    }
}