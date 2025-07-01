using Awaken.Utility.Files;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.Editor.Graphics.ProceduralMeshes.TerrainToMeshConverter {
    [CreateAssetMenu(fileName = "TerrainToMeshMaterial", menuName = "TG/Terrain/TerrainToMesh Material")]
    public class TerrainToMeshMaterial : ScriptableObject {
        [SerializeField] Shader shader;
        [SerializeField] string[] keywords = Array.Empty<string>();
        [SerializeField] float heightTransition = 0.1f;
        [SerializeField] Layer[] customLayers = Array.Empty<Layer>();

        public (Material material, Texture2D[] splatmaps) Create(List<TerrainToMesh.AssetToCreate> toCreate, TerrainData data, in TerrainToMesh.PersistenceInfo persistenceInfo) {
            var material = new Material(shader);

            material.SetFloat(Id.SplatResolution, data.size.x);
            material.SetFloat(Id.HeightTransition, heightTransition);

            foreach (var keyword in keywords) {
                if (Array.IndexOf(shader.keywordSpace.keywordNames, keyword) >= 0) {
                    material.SetKeyword(new LocalKeyword(shader, keyword), true);
                } else {
                    Log.Critical?.Error($"Terrain shader {shader.name} does not have {keyword} keyword", this);
                }
            }

            var alphamaps = data.alphamapTextures.CreateCopy();
            for (var i = 0; i < math.min(alphamaps.Length, 2); i++) {
                alphamaps[i] = EditorAssetUtil.Create(alphamaps[i], persistenceInfo.folder, $"{persistenceInfo.name}_Splat{i}");
                material.SetTexture(Id.Splatmap[i], alphamaps[i]);
            }

            var terrainLayers = data.terrainLayers;
            for (var i = 0; i < math.min(terrainLayers.Length, 8); i++) {
                var terrainLayer = terrainLayers[i];
                material.SetTexture(Id.BaseColor[i], terrainLayer.diffuseTexture);
                material.SetTexture(Id.Normal[i], terrainLayer.normalMapTexture);
                material.SetTexture(Id.MaskMap[i], terrainLayer.maskMapTexture);
                material.SetVector(Id.BaseColorRemapMin[i], terrainLayer.diffuseRemapMin);
                material.SetVector(Id.BaseColorRemapMax[i], terrainLayer.diffuseRemapMax);
                material.SetVector(Id.MaskMapRemapMin[i], terrainLayer.maskMapRemapMin);
                material.SetVector(Id.MaskMapRemapMax[i], terrainLayer.maskMapRemapMax);
                material.SetFloat(Id.NormalStrength[i], terrainLayer.normalScale);
                material.SetFloat(Id.Tiling[i], terrainLayer.tileSize.x);
            }

            persistenceInfo.RequestMaterialAssetCreation(toCreate, material);
            return (material, alphamaps);
        }

        [Serializable]
        class Layer {
            [SerializeField] TerrainLayer layer;
            [SerializeField] Color tint = Color.white;
            [SerializeField] float amplitude = 1;

            [SerializeField, ToggleLeft, LabelText("Normal Strength"), HorizontalGroup("NormalStrength")] bool overrideNormalStrength;
            [SerializeField, ToggleLeft, LabelText("Tiling"), HorizontalGroup("Tiling")] bool overrideTiling;

            [SerializeField, HideLabel, HorizontalGroup("NormalStrength")] float normalStrength = 1;
            [SerializeField, HideLabel, HorizontalGroup("Tiling")] float tiling = 4;

            public Texture2D BaseColor => layer.diffuseTexture;
            public Texture2D Normal => layer.normalMapTexture;
            public Texture2D MaskMap => layer.maskMapTexture;
            public Color Tint => tint;
            public float Amplitude => amplitude;
            public float NormalStrength => overrideNormalStrength ? normalStrength : layer.normalScale;
            public float Tiling => overrideTiling ? tiling : layer.tileSize.x;

            public bool IsFor(TerrainLayer terrainLayer) => layer == terrainLayer;
        }

        static class Id {
            public static readonly int SplatResolution = Shader.PropertyToID("_SplatResolution");
            public static readonly int HeightTransition = Shader.PropertyToID("_HeightTransition");

            public static readonly int[] Splatmap = {
                Shader.PropertyToID("_Splat0"),
                Shader.PropertyToID("_Splat1"),
            };

            public static readonly int[] BaseColor = {
                Shader.PropertyToID("_L0_BaseColor"),
                Shader.PropertyToID("_L1_BaseColor"),
                Shader.PropertyToID("_L2_BaseColor"),
                Shader.PropertyToID("_L3_BaseColor"),
                Shader.PropertyToID("_L4_BaseColor"),
                Shader.PropertyToID("_L5_BaseColor"),
                Shader.PropertyToID("_L6_BaseColor"),
                Shader.PropertyToID("_L7_BaseColor"),
            };

            public static readonly int[] Normal = {
                Shader.PropertyToID("_L0_Normal"),
                Shader.PropertyToID("_L1_Normal"),
                Shader.PropertyToID("_L2_Normal"),
                Shader.PropertyToID("_L3_Normal"),
                Shader.PropertyToID("_L4_Normal"),
                Shader.PropertyToID("_L5_Normal"),
                Shader.PropertyToID("_L6_Normal"),
                Shader.PropertyToID("_L7_Normal"),
            };

            public static readonly int[] MaskMap = {
                Shader.PropertyToID("_L0_MaskMap"),
                Shader.PropertyToID("_L1_MaskMap"),
                Shader.PropertyToID("_L2_MaskMap"),
                Shader.PropertyToID("_L3_MaskMap"),
                Shader.PropertyToID("_L4_MaskMap"),
                Shader.PropertyToID("_L5_MaskMap"),
                Shader.PropertyToID("_L6_MaskMap"),
                Shader.PropertyToID("_L7_MaskMap"),
            };

            public static readonly int[] NormalStrength = {
                Shader.PropertyToID("_L0_NormalStrength"),
                Shader.PropertyToID("_L1_NormalStrength"),
                Shader.PropertyToID("_L2_NormalStrength"),
                Shader.PropertyToID("_L3_NormalStrength"),
                Shader.PropertyToID("_L4_NormalStrength"),
                Shader.PropertyToID("_L5_NormalStrength"),
                Shader.PropertyToID("_L6_NormalStrength"),
                Shader.PropertyToID("_L7_NormalStrength"),
            };

            public static readonly int[] Tiling = {
                Shader.PropertyToID("_L0_Tiling"),
                Shader.PropertyToID("_L1_Tiling"),
                Shader.PropertyToID("_L2_Tiling"),
                Shader.PropertyToID("_L3_Tiling"),
                Shader.PropertyToID("_L4_Tiling"),
                Shader.PropertyToID("_L5_Tiling"),
                Shader.PropertyToID("_L6_Tiling"),
                Shader.PropertyToID("_L7_Tiling"),
            };

            public static readonly int[] BaseColorRemapMin = {
                Shader.PropertyToID("_L0_BaseColorRemapMin"),
                Shader.PropertyToID("_L1_BaseColorRemapMin"),
                Shader.PropertyToID("_L2_BaseColorRemapMin"),
                Shader.PropertyToID("_L3_BaseColorRemapMin"),
                Shader.PropertyToID("_L4_BaseColorRemapMin"),
                Shader.PropertyToID("_L5_BaseColorRemapMin"),
                Shader.PropertyToID("_L6_BaseColorRemapMin"),
                Shader.PropertyToID("_L7_BaseColorRemapMin"),
            };

            public static readonly int[] BaseColorRemapMax = {
                Shader.PropertyToID("_L0_BaseColorRemapMax"),
                Shader.PropertyToID("_L1_BaseColorRemapMax"),
                Shader.PropertyToID("_L2_BaseColorRemapMax"),
                Shader.PropertyToID("_L3_BaseColorRemapMax"),
                Shader.PropertyToID("_L4_BaseColorRemapMax"),
                Shader.PropertyToID("_L5_BaseColorRemapMax"),
                Shader.PropertyToID("_L6_BaseColorRemapMax"),
                Shader.PropertyToID("_L7_BaseColorRemapMax"),
            };
            

            public static readonly int[] MaskMapRemapMin = {
                Shader.PropertyToID("_L0_MaskMapRemapMin"),
                Shader.PropertyToID("_L1_MaskMapRemapMin"),
                Shader.PropertyToID("_L2_MaskMapRemapMin"),
                Shader.PropertyToID("_L3_MaskMapRemapMin"),
                Shader.PropertyToID("_L4_MaskMapRemapMin"),
                Shader.PropertyToID("_L5_MaskMapRemapMin"),
                Shader.PropertyToID("_L6_MaskMapRemapMin"),
                Shader.PropertyToID("_L7_MaskMapRemapMin"),
            };

            public static readonly int[] MaskMapRemapMax = {
                Shader.PropertyToID("_L0_MaskMapRemapMax"),
                Shader.PropertyToID("_L1_MaskMapRemapMax"),
                Shader.PropertyToID("_L2_MaskMapRemapMax"),
                Shader.PropertyToID("_L3_MaskMapRemapMax"),
                Shader.PropertyToID("_L4_MaskMapRemapMax"),
                Shader.PropertyToID("_L5_MaskMapRemapMax"),
                Shader.PropertyToID("_L6_MaskMapRemapMax"),
                Shader.PropertyToID("_L7_MaskMapRemapMax"),
            };
        }
    }
}
