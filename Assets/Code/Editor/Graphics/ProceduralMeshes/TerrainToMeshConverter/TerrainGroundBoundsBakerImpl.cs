using System;
using System.Collections.Generic;
using Awaken.TG.Main.Grounds;
using Awaken.Utility.Collections;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TerrainTools;

namespace Awaken.TG.Editor.Graphics.ProceduralMeshes.TerrainToMeshConverter {
    [CreateAssetMenu(fileName = "TerrainGroundBoundsBaker", menuName = "TG/Terrain/TerrainGroundBoundsBaker")]
    public class TerrainGroundBoundsBakerImpl : TerrainGroundBoundsBaker.Impl {
        [SerializeField] TerrainResolutions gameplayResolutions;
        [SerializeField] TerrainResolutions foregroundResolutions;
        [SerializeField] TerrainResolutions backgroundResolutions;
        [SerializeField] TerrainToMeshMaterial terrainToMeshMaterial;
        
        public override void Bake(GroundBounds groundBounds, TerrainGroundBoundsBaker baker) {
            var sceneName = baker.gameObject.scene.name;

            groundBounds.CalculateGamePolygon(Allocator.Temp, out var gamePolygon);
            groundBounds.CalculateTerrainForegroundPolygon(Allocator.Temp, out var terrainForegroundPolygon);
            groundBounds.CalculateTerrainBackgroundPolygon(Allocator.Temp, out var terrainBackgroundPolygon);

            var terrains = baker.GetComponentsInChildren<Terrain>();
            var gameplayTerrainsBounds = MinMaxAABR.Empty;
            var gameplayTerrains = new ListDictionary<float, UnsafePinnableList<Terrain>>(terrains.Length);

            var toCreate = new List<TerrainToMesh.AssetToCreate>();
            for (int i = 0; i < terrains.Length; i++) {
                Terrain terrain = terrains[i];
                var terrainLocalBounds = terrain.terrainData.bounds;
                var terrainWorldBounds = terrainLocalBounds.Transform(terrain.transform.localToWorldMatrix);
                var terrainBounds = new MinMaxAABR(terrainWorldBounds);
                
                Polygon2DUtils.Intersects(terrainBounds, gamePolygon, out var intersectsGame);
                if (intersectsGame) {
                    var size = terrain.terrainData.size.x;
                    ref var terrainsList =
                        ref gameplayTerrains.TryGetOrCreateValue(size, out var groupIndex, out var created);
                    if (created) {
                        terrainsList = new UnsafePinnableList<Terrain>(terrains.Length - i);
                    }
                    terrainsList.Add(terrain);

                    gameplayTerrainsBounds.Encapsulate(terrain.transform.position.xz());

                    SetupTerrain(toCreate, terrain, gameplayResolutions, true, sceneName);
                    continue;
                }
                
                Polygon2DUtils.Intersects(terrainBounds, terrainForegroundPolygon, out var intersectsForeground);
                if (intersectsForeground) {
                    SetupTerrain(toCreate, terrain, foregroundResolutions, false, sceneName);
                    continue;
                }

                Polygon2DUtils.Intersects(terrainBounds, terrainBackgroundPolygon, out var intersectsBackground);
                if (intersectsBackground) {
                    SetupTerrain(toCreate, terrain, backgroundResolutions, false, sceneName);
                    continue;
                }

                DestroyImmediate(terrain.gameObject);
            }

            TerrainToMesh.AssetToCreate.Create(toCreate);
            gamePolygon.Dispose();
            terrainForegroundPolygon.Dispose();
            terrainBackgroundPolygon.Dispose();
        }
        
        void SetupTerrain(List<TerrainToMesh.AssetToCreate> toCreate, Terrain terrain, in TerrainResolutions resolutions, bool withFootsteps, string sceneName) {
            var terrainData = terrain.terrainData;
            terrainData = CopyTerrainData(terrainData, sceneName);

            var splatmapSize = math.ceilpow2((int)(math.rcp(resolutions.splatmapMetersPerPixel) * terrainData.size.x));
            if (splatmapSize < terrainData.alphamapResolution) {
                ResizeControlTexture(splatmapSize, terrainData);
            }
            
            terrain.terrainData = terrainData;
            terrain.Flush();
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(terrainData);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(terrainData);
#endif
            
            var terrainToMeshConfig = new TerrainToMesh.Config {
                mesh = resolutions.terrainToMeshMesh,
                material = terrainToMeshMaterial,
                withFootsteps = withFootsteps
            };
            TerrainToMesh.Convert(toCreate, terrain, terrainToMeshConfig);
        }

        TerrainData CopyTerrainData(TerrainData old, string sceneName) {
            var originalPath = UnityEditor.AssetDatabase.GetAssetPath(old);
            string newPath;
            if (!originalPath.Contains(sceneName)) {
                newPath = originalPath.Replace(".asset", $"_{sceneName}.asset");
            } else {
                newPath = originalPath.Replace(".asset", "_Copy.asset");
            }
            UnityEditor.AssetDatabase.CopyAsset(originalPath, newPath);
            return UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainData>(newPath);
        }

        [Serializable]
        struct TerrainResolutions {
            public int splatmapMetersPerPixel;
            public TerrainToMeshMesh terrainToMeshMesh;
        }

        void ResizeControlTexture(int newResolution, TerrainData terrainData) {
            RenderTexture oldRT = RenderTexture.active;

            // we record the terrainData because we change terrainData.alphamapResolution
            // we also store a complete copy of the alphamap -- because each alphamap is a separate asset, these are separate
            var undoObjects = new List<UnityEngine.Object>();
            undoObjects.Add(terrainData);
            undoObjects.AddRange(terrainData.alphamapTextures);
            UnityEditor.Undo.RegisterCompleteObjectUndo(undoObjects.ToArray(), "Resize Alphamap");

            Material blitMaterial =
                TerrainPaintUtility
                    .GetCopyTerrainLayerMaterial(); // special blit that forces copy from highest mip only

            int targetRezU = newResolution;
            int targetRezV = newResolution;

            float invTargetRezU = 1.0f / targetRezU;
            float invTargetRezV = 1.0f / targetRezV;

            RenderTexture[] resizedAlphaMaps = new RenderTexture[terrainData.alphamapTextureCount];
            for (int i = 0; i < resizedAlphaMaps.Length; i++) {
                Texture2D oldAlphamap = terrainData.alphamapTextures[i];

                int sourceRezU = oldAlphamap.width;
                int sourceRezV = oldAlphamap.height;
                float invSourceRezU = 1.0f / sourceRezU;
                float invSourceRezV = 1.0f / sourceRezV;

                resizedAlphaMaps[i] =
                    RenderTexture.GetTemporary(newResolution, newResolution, 0, oldAlphamap.graphicsFormat);

                float scaleU = (1.0f - invSourceRezU) / (1.0f - invTargetRezU);
                float scaleV = (1.0f - invSourceRezV) / (1.0f - invTargetRezV);
                float offsetU = 0.5f * (invSourceRezU - scaleU * invTargetRezU);
                float offsetV = 0.5f * (invSourceRezV - scaleV * invTargetRezV);

                Vector2 scale = new Vector2(scaleU, scaleV);
                Vector2 offset = new Vector2(offsetU, offsetV);

                blitMaterial.mainTexture = oldAlphamap;
                blitMaterial.mainTextureScale = scale;
                blitMaterial.mainTextureOffset = offset;

                // custom blit
                oldAlphamap.filterMode = FilterMode.Bilinear;
                RenderTexture.active = resizedAlphaMaps[i];
                GL.PushMatrix();
                GL.LoadPixelMatrix(0, newResolution, 0, newResolution);
                blitMaterial.SetPass(2);
                DrawQuad(new RectInt(0, 0, newResolution, newResolution), new RectInt(0, 0, sourceRezU, sourceRezV), oldAlphamap);
                GL.PopMatrix();
            }

            terrainData.alphamapResolution = newResolution;
            for (int i = 0; i < resizedAlphaMaps.Length; i++) {
                RenderTexture.active = resizedAlphaMaps[i];
                terrainData.CopyActiveRenderTextureToTexture(TerrainData.AlphamapTextureName, i,
                    new RectInt(0, 0, newResolution, newResolution), Vector2Int.zero, false);
            }
            terrainData.SetBaseMapDirty();
            RenderTexture.active = oldRT;
            for (int i = 0; i < resizedAlphaMaps.Length; i++) {
                RenderTexture.ReleaseTemporary(resizedAlphaMaps[i]);
            }
        }

        // -- TerrainPaintUtility
        static void DrawQuad(RectInt destinationPixels, RectInt sourcePixels, Texture sourceTexture) {
            DrawQuad2(destinationPixels, sourcePixels, sourceTexture, sourcePixels, sourceTexture);
        }

        static void DrawQuad2(RectInt destinationPixels, RectInt sourcePixels, Texture sourceTexture,
            RectInt sourcePixels2, Texture sourceTexture2) {
            if (destinationPixels.width <= 0 || destinationPixels.height <= 0) {
                return;
            }
            Rect rect1 = new Rect((float) sourcePixels.x / (float) sourceTexture.width, (float) sourcePixels.y / (float) sourceTexture.height, (float) sourcePixels.width / (float) sourceTexture.width, (float) sourcePixels.height / (float) sourceTexture.height);
            Rect rect2 = new Rect((float) sourcePixels2.x / (float) sourceTexture2.width, (float) sourcePixels2.y / (float) sourceTexture2.height, (float) sourcePixels2.width / (float) sourceTexture2.width, (float) sourcePixels2.height / (float) sourceTexture2.height);
            GL.Begin(7);
            GL.Color(new Color(1f, 1f, 1f, 1f));
            GL.MultiTexCoord2(0, rect1.x, rect1.y);
            GL.MultiTexCoord2(1, rect2.x, rect2.y);
            GL.Vertex3((float) destinationPixels.x, (float) destinationPixels.y, 0.0f);
            GL.MultiTexCoord2(0, rect1.x, rect1.yMax);
            GL.MultiTexCoord2(1, rect2.x, rect2.yMax);
            GL.Vertex3((float) destinationPixels.x, (float) destinationPixels.yMax, 0.0f);
            GL.MultiTexCoord2(0, rect1.xMax, rect1.yMax);
            GL.MultiTexCoord2(1, rect2.xMax, rect2.yMax);
            GL.Vertex3((float) destinationPixels.xMax, (float) destinationPixels.yMax, 0.0f);
            GL.MultiTexCoord2(0, rect1.xMax, rect1.y);
            GL.MultiTexCoord2(1, rect2.xMax, rect2.y);
            GL.Vertex3((float) destinationPixels.xMax, (float) destinationPixels.y, 0.0f);
            GL.End();
        }
    }
}