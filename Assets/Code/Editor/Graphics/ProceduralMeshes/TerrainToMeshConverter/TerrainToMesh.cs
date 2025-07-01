using System;
using System.Collections.Generic;
using Awaken.ECS.MedusaRenderer;
using Awaken.TG.Editor.Utility.Assets;
using Awaken.TG.Main.Heroes.FootSteps;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Maths;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Renderer = UnityEngine.Renderer;

namespace Awaken.TG.Editor.Graphics.ProceduralMeshes.TerrainToMeshConverter {
    public static class TerrainToMesh {
        public static void Convert(List<AssetToCreate> toCreate, Terrain terrain, in Config config) {
            var terrainGO = terrain.gameObject;
            var terrainTransform = terrainGO.transform;
            
            var name = terrainGO.name;
            var data = terrain.terrainData;
            var persistenceInfo = GetPersistenceInfo(terrainGO.scene.name, data);
            var lodsToSpawn = config.mesh.Create(toCreate, data, persistenceInfo);
            var (material, splatmaps) = config.material.Create(toCreate, data, persistenceInfo);
            
            var meshGO = new GameObject(name);
            meshGO.isStatic = true;
            meshGO.layer = RenderLayers.Terrain;
            var meshTransform = meshGO.transform;
            meshTransform.SetParent(terrainTransform.parent);
            meshTransform.CopyPositionAndRotationFrom(terrainTransform);

            var terrainPosition = terrainTransform.position;
            var terrainSize = data.size;
            
            Object.DestroyImmediate(terrainGO);
            
            var lods = new LOD[lodsToSpawn.Length];
            for (int i = 0; i < lodsToSpawn.Length; i++) {
                ref readonly var lodToSpawn = ref lodsToSpawn[i];
                
                var lodGO = new GameObject($"{name}_LOD{i}");
                lodGO.isStatic = true;
                lodGO.layer = RenderLayers.Terrain;
                var lodTransform = lodGO.transform;
                lodTransform.SetParent(meshTransform, false);
                
                var count = lodToSpawn.meshes.Length;
                var renderers = new Renderer[count];
                for (int j = 0; j < count; j++) {
                    var lodPartGO = new GameObject($"{name}_LOD{i}_{j}");
                    lodPartGO.isStatic = true;
                    lodPartGO.layer = RenderLayers.Terrain;
                    var lodPartTransform = lodPartGO.transform;
                    lodPartTransform.SetParent(lodTransform, false);
                    
                    var meshFilter = lodPartGO.AddComponent<MeshFilter>();
                    var meshRenderer = lodPartGO.AddComponent<MeshRenderer>();

                    meshFilter.sharedMesh = lodToSpawn.meshes[j];
                    meshRenderer.sharedMaterial = material;
                    
                    renderers[j] = meshRenderer;
                }
                
                lods[i] = new LOD(lodToSpawn.screenRelativeTransitionHeight, renderers);
            }

            var lodGroup = meshGO.AddComponent<LODGroup>();
            lodGroup.SetLODs(lods);
            
            meshGO.AddComponent<MedusaRendererPrefab>();

            if (config.withFootsteps) {
                var surfaceMap = EditorTextureToFootstepMap.Get;
                var footsteps = meshGO.AddComponent<MeshTerrainFootstepSource>();
                var accessor = new MeshTerrainFootstepSource.EditorAccessor(footsteps);
                accessor.FmodParameters = ArrayUtils.Select(data.terrainLayers, surfaceMap.FindFmodParameter);
                accessor.Splatmaps = splatmaps;
                accessor.ChunkStart = terrainPosition.xz();
                accessor.ChunkEnd = terrainPosition.xz() + terrainSize.xz();
            }

            if (config.mesh.CollisionLod >= 0) {
                ref readonly var colliderLodData = ref lodsToSpawn[config.mesh.CollisionLod];
                for (int i = 0; i < colliderLodData.meshes.Length; i++) {
                    var meshCollider = meshGO.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = colliderLodData.meshes[i];
                }
            }
        }

        static PersistenceInfo GetPersistenceInfo(string sceneName, TerrainData data) {
            var name = data.name;
            
            var sceneFolder = $"Assets/3DAssets/Terrain/_Baking/{sceneName}";
            if (!AssetDatabase.IsValidFolder(sceneFolder)) {
                AssetDatabase.CreateFolder("Assets/3DAssets/Terrain/_Baking", sceneName);
            }
            
            return new PersistenceInfo(sceneFolder, name);
        }

        public readonly struct PersistenceInfo {
            public readonly string folder;
            public readonly string name;

            public PersistenceInfo(string folder, string name) {
                this.folder = folder;
                this.name = name;
            }

            public void RequestMaterialAssetCreation(List<AssetToCreate> toCreate, Material material) {
                toCreate.Add(new AssetToCreate {
                    asset = material,
                    path = $"{folder}/Mat_{name}.mat"
                });
            }

            public void RequestMeshAssetCreation(List<AssetToCreate> toCreate, Mesh mesh, int lod, int index) {
                toCreate.Add(new AssetToCreate {
                    asset = mesh,
                    path = $"{folder}/Mesh_{name}_LOD{lod}_{index}.mesh"
                });
            }
        }

        public struct AssetToCreate {
            public Object asset;
            public string path;

            public static void Create(List<AssetToCreate> toCreate) {
                using var scope = new AssetsUtils.AssetEditingScope(true);
                foreach (var assetToCreate in toCreate) {
                    AssetDatabase.CreateAsset(assetToCreate.asset, assetToCreate.path);
                }
                toCreate.Clear();
            }
        }

        [Serializable]
        public struct Config {
            public TerrainToMeshMesh mesh;
            public TerrainToMeshMaterial material;
            public bool withFootsteps;
        }
    }
}