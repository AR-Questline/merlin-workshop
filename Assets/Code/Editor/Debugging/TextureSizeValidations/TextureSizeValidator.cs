using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.Kandra;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Debugging.GUIDSearching;
using Awaken.TG.Editor.Utility.Paths;
using Awaken.TG.Main.General;
using Awaken.TG.Utility;
using Awaken.Utility.Assets;
using Awaken.Utility.Collections;
using Awaken.Utility.UI;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.Editor.Debugging.TextureSizeValidations {
    public class TextureSizeValidator : OdinEditorWindow {
        static readonly HashSet<Texture2D> ReusableTextureSet = new();
        
        static Shader s_HDRPLit;
        
        [SerializeField, HideLabel, InlineProperty, FoldoutGroup("Settings")] SharedSettings settings;
        
        [SerializeField, HideInInspector] List<GameObject> prefabs = new();
        [SerializeField, TableList(AlwaysExpanded = true, DrawScrollView = false, CellPadding = 5)] List<Result> results;
        
        [MenuItem("TG/Assets/Textures/Size Validator")]
        static void OpenWindow() {
            GetWindow<TextureSizeValidator>().Show();
        }

        protected override void OnEnable() {
            base.OnEnable();
            
            s_HDRPLit = Shader.Find("HDRP/Lit");
            
            GUIDCache.Load();
        }

        [Button]
        void Clear() {
            prefabs.Clear();
            results.Clear();
        }
        
        protected override void OnImGUI() {
            SharedSettings.Instance = settings;
            DragNDropBox();
            base.OnImGUI();
            SharedSettings.Instance = null;
        }

        void DragNDropBox() {
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag Prefabs Here");

            Event evt = Event.current;

            if (!dropArea.Contains(evt.mousePosition)) {
                return;
            }

            switch (evt.type) {
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    evt.Use();
                    break;
                case EventType.DragPerform:
                    DragAndDrop.AcceptDrag();
                    foreach (var draggedObject in DragAndDrop.objectReferences) {
                        GameObject prefab = draggedObject as GameObject;
                        if (prefab != null && PrefabUtility.GetPrefabAssetType(prefab) != PrefabAssetType.NotAPrefab) {
                            AddPrefab(prefab);
                        }
                    }
                    evt.Use();
                    break;
            }
        }

        void AddPrefab(GameObject prefab) {
            if (prefab == null) {
                return;
            }

            if (prefabs.Contains(prefab)) {
                return;
            }

            prefabs.Add(prefab);

            foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>()) {
                var localToWorld = renderer.localToWorldMatrix;
                var materials = renderer.sharedMaterials;
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                ProcessMaterials(localToWorld, renderer.gameObject, mesh, materials);
            }

            foreach (var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                var localToWorld = renderer.localToWorldMatrix;
                var materials = renderer.sharedMaterials;
                var mesh = renderer.sharedMesh;
                ProcessMaterials(localToWorld, renderer.gameObject, mesh, materials);
            }

            foreach (var renderer in prefab.GetComponentsInChildren<DrakeMeshRenderer>()) {
                var localToWorld = renderer.transform.localToWorldMatrix * (Matrix4x4)renderer.LocalToWorldOffset;
                var materials = ArrayUtils.Select(renderer.MaterialReferences, materialReference => materialReference.EditorLoad<Material>());
                var mesh = renderer.MeshReference.EditorLoad<Mesh>();
                ProcessMaterials(localToWorld, renderer.gameObject, mesh, materials);
            }

            foreach (var renderer in prefab.GetComponentsInChildren<KandraRenderer>()) {
                var localToWorld = renderer.rendererData.rig.bones[renderer.rendererData.rootBone].localToWorldMatrix;
                var materials = renderer.rendererData.materials;
                var mesh = renderer.rendererData.EDITOR_sourceMesh;
                ProcessMaterials(localToWorld, renderer.gameObject, mesh, materials);
            }

            void ProcessMaterials(Matrix4x4 localToWorld, GameObject gameObject, Mesh mesh, Material[] materials) {
                if (mesh == null) {
                    return;
                }
                var vertices = mesh.vertices;
                var uvs = mesh.uv;
                var lastSubmesh = mesh.subMeshCount - 1;
                for (int i = 0; i < materials.Length; i++) {
                    var material = materials[i];
                    if (material == null) {
                        continue;
                    }
                    var locationData = new LocationData(prefab, gameObject, mesh, material);
                    
                    var indices = mesh.GetTriangles(math.min(i, lastSubmesh));
                    var geometryData = ComputeGeometryData(localToWorld, vertices, uvs, indices);
                    
                    var textureData = new List<TextureData>();
                    AddTextures(textureData, material, TextureType.Albedo, Id.Albedos);
                    AddTextures(textureData, material, TextureType.Normal, Id.Normals);
                    AddTextures(textureData, material, TextureType.Mask, Id.MaskMaps);
                    AddTextures(textureData, material, TextureType.Invalid, Id.Invalids);

                    var unhandledTextures = new List<string>();
                    HandleUnhandledTextures(unhandledTextures, material);
                    
                    results.Add(new Result(new MeshData(locationData, geometryData), textureData, unhandledTextures));
                }
            }
        }

        static void AddTextures(List<TextureData> list, Material material, TextureType type, int[] ids) {
            foreach (var id in ids) {
                if (TryGetTexture(material, id, out var texture)) {
                    if (ReusableTextureSet.Add(texture)) {
                        list.Add(new TextureData {
                            texture = texture,
                            type = type,
                        });
                    }
                }
            }
            ReusableTextureSet.Clear();
        }

        static bool TryGetTexture(Material material, int id, out Texture2D texture) {
            if (material.HasTexture(id)) {
                texture = material.GetTexture(id) as Texture2D;
            } else {
                texture = null;
            }
            return texture != null;
        }
        
        static void HandleUnhandledTextures(List<string> unhandledTextures, Material material) {
            var shader = material.shader;
            var propertyCount = shader.GetPropertyCount();
            for (int i = 0; i < propertyCount; i++) {
                var type = shader.GetPropertyType(i);
                if (type != ShaderPropertyType.Texture) {
                    continue;
                }
                var id = shader.GetPropertyNameId(i);
                if (Array.IndexOf(Id.All, id) != -1) {
                    continue;
                }
                unhandledTextures.Add(shader.GetPropertyName(i));
            }
        }
        
        static GeometryData ComputeGeometryData(Matrix4x4 localToWorld, Vector3[] vertices, Vector2[] uvs, int[] indices) {
            ComputeTexelData(localToWorld, vertices, uvs, indices, out var uvDensityDeviationPercent, out float texelDensityFactor);
            ComputeCoverageData(uvs, indices, out var coverage);
            return new GeometryData {
                uvDensityDeviationPercent = uvDensityDeviationPercent,
                texelDensityFactor = texelDensityFactor,
                uvCoverage = coverage,
            };
        }

        static void ComputeTexelData(Matrix4x4 localToWorld, Vector3[] vertices, Vector2[] uvs, int[] indices, out float uvDensityDeviationPercent, out float texelDensityFactor) {
            const float OneOverMetersToOneOverCentimeters = 0.01f;
            
            if (indices.Length == 0) {
                uvDensityDeviationPercent = 0;
                texelDensityFactor = 0;
                return;
            }
            
            var uvDensities = new float[indices.Length / 3];

            var uvAreaSum = 0f;
            var worldAreaSum = 0f;
            
            var uvDensityAverage = 0f;
            for (int i = 0; i < indices.Length; i += 3) {
                var world0 = localToWorld.MultiplyPoint(vertices[indices[i]]);
                var world1 = localToWorld.MultiplyPoint(vertices[indices[i + 1]]);
                var world2 = localToWorld.MultiplyPoint(vertices[indices[i + 2]]);
                var uv0 = uvs[indices[i]];
                var uv1 = uvs[indices[i + 1]];
                var uv2 = uvs[indices[i + 2]];

                var uvArea = math.abs(math.cross((uv1 - uv0).ToHorizontal3(), (uv2 - uv0).ToHorizontal3()).y) * 0.5f;
                var worldArea = math.length(math.cross(world1 - world0, world2 - world0)) * 0.5f;
                
                uvAreaSum += uvArea;
                worldAreaSum += worldArea;
                
                var uvDensity = uvArea / worldArea;
                uvDensities[i / 3] = uvDensity;
                uvDensityAverage += uvDensity;
            }
            uvDensityAverage /= uvDensities.Length;
            texelDensityFactor = math.sqrt(uvAreaSum / worldAreaSum) * OneOverMetersToOneOverCentimeters;
            
            var uvDensityDeviation = 0f;
            for (int i = 0; i < uvDensities.Length; i++) {
                uvDensityDeviation += math.square(uvDensities[i] - uvDensityAverage);
            }
            uvDensityDeviation = math.sqrt(uvDensityDeviation / uvDensities.Length);
            
            uvDensityDeviationPercent = uvDensityDeviation / uvDensityAverage;
        }

        static void ComputeCoverageData(Vector2[] uvs, int[] indices, out float coverage) {
            const int TextureCoverageSamplingResolution = 100;
            
            bool[,] isSampled = new bool[TextureCoverageSamplingResolution + 1, TextureCoverageSamplingResolution + 1];
            
            for (int i = 0; i < indices.Length; i += 3) {
                var uv0 = uvs[indices[i]];
                var uv1 = uvs[indices[i + 1]];
                var uv2 = uvs[indices[i + 2]];

                var uvMin = math.min(math.min(uv0, uv1), uv2);
                var uvMax = math.max(math.max(uv0, uv1), uv2);
                
                var discreteUvMin = (int2) math.floor(uvMin * TextureCoverageSamplingResolution);
                var discreteUvMax = (int2) math.ceil(uvMax * TextureCoverageSamplingResolution);

                for (int discreteX = discreteUvMin.x; discreteX <= discreteUvMax.x; discreteX++) {
                    for (int discreteY = discreteUvMin.y; discreteY <= discreteUvMax.y; discreteY++) {
                        float x = discreteX / (float) TextureCoverageSamplingResolution;
                        float y = discreteY / (float) TextureCoverageSamplingResolution;
                        if (Algorithms2D.InsideOfTriangle(uv0, uv1, uv2, new Vector3(x, y))) {
                            int wrappedX = (discreteX + (TextureCoverageSamplingResolution + 1)) % (TextureCoverageSamplingResolution + 1);
                            int wrappedY = (discreteY + (TextureCoverageSamplingResolution + 1)) % (TextureCoverageSamplingResolution + 1);
                            isSampled[wrappedX, wrappedY] = true;
                        }
                    }
                }
            }
            
            int sampledCount = 0;
            for (int x = 0; x < TextureCoverageSamplingResolution + 1; x++) {
                for (int y = 0; y < TextureCoverageSamplingResolution + 1; y++) {
                    sampledCount += isSampled[x, y] ? 1 : 0;
                }
            }
            
            coverage = sampledCount / (float) ((TextureCoverageSamplingResolution + 1) * (TextureCoverageSamplingResolution + 1));
        }
        
        [Serializable]
        class MeshData {
            [HideInInspector] public LocationData location;
            [HideInInspector] public GeometryData geometry;

            [ShowInInspector, EnableGUI]
            public GameObject Prefab => location.prefab;

            [ShowInInspector, EnableGUI]
            public GameObject GameObject => location.gameObject;

            [ShowInInspector, EnableGUI]
            public Mesh Mesh => location.mesh;

            [ShowInInspector, EnableGUI, BoxGroup("Material"), HorizontalGroup("Material/Row"), HideLabel, GUIColor(nameof(GetMaterialColor))]
            public Material Material => location.material;

            [ShowInInspector, EnableGUI, BoxGroup("Material"), HorizontalGroup("Material/Row"), HideLabel, GUIColor(nameof(GetShaderColor))]
            public Shader Shader => location.shader;

            [ShowInInspector, EnableGUI, BoxGroup("Material"), LabelText("Usages"), GUIColor(nameof(GetMaterialUsagesColor))]
            public GameObject[] MaterialUsages => location.materialUsages;

            [ShowInInspector, EnableGUI, GUIColor(nameof(GetUvDensityDeviationColor))]
            [Tooltip("If this value is high it means that some parts of mesh are more detailed then others")]
            public float UvDeviation => geometry.uvDensityDeviationPercent;

            [ShowInInspector, EnableGUI, GUIColor(nameof(GetCoverageColor))]
            [Tooltip("Percent of texture that is covered by uv")]
            public float UvCoverage => geometry.uvCoverage;

            public MeshData(LocationData location, GeometryData geometry) {
                this.location = location;
                this.geometry = geometry;
            }

            Color GetMaterialColor() {
                var shaderColor = GetShaderColor();
                var materialUsagesColor = GetMaterialUsagesColor();
                if (shaderColor == materialUsagesColor) {
                    return shaderColor;
                }
                return GUIColors.White;
            }
            
            Color GetShaderColor() {
                if (Shader == s_HDRPLit){
                    return GUIColors.Green;
                } else {
                    return GUIColors.White;
                }
            }

            Color GetMaterialUsagesColor() {
                if (MaterialUsages.Length == 1){
                    return GUIColors.Green;
                } else {
                    return GUIColors.White;
                }
            }

            Color GetUvDensityDeviationColor() {
                if (UvDeviation > SharedSettings.Instance.maxUvDeviation) {
                    return GUIColors.Red;
                } else {
                    return GUIColors.White;
                }
            }

            Color GetCoverageColor() {
                if (UvCoverage < SharedSettings.Instance.minTextureCoverage) {
                    return GUIColors.Red;
                } else {
                    return GUIColors.White;
                }
            }
        }

        [Serializable]
        class LocationData {
            public GameObject prefab;
            public GameObject gameObject;
            public Mesh mesh;
            public Material material;
            public Shader shader;
            public GameObject[] materialUsages;

            public LocationData(GameObject prefab, GameObject gameObject, Mesh mesh, Material material) {
                this.prefab = prefab;
                this.gameObject = gameObject;
                this.mesh = mesh;
                this.material = material;
                
                shader = material.shader;
                materialUsages = GUIDCache.Instance.GetDependent(material)
                    .Where(path => path.EndsWith(".prefab"))
                    .Select(path => {
                        path = path.StartsWith("Assets") ? path : PathUtils.FilesystemToAssetPath(path);
                        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    })
                    .ToArray();
            }
        }

        [Serializable]
        class GeometryData {
            public float uvDensityDeviationPercent;
            public float texelDensityFactor;
            public float uvCoverage;
        }

        [Serializable]
        class TextureData {
            [HideInInspector] public Texture2D texture;
            [HideInInspector] public TextureType type;
            [HideInInspector] public GeometryData geometryData;

            [GUIColor(nameof(GetGUIColor))]
            [ShowInInspector, EnableGUI, HideLabel, VerticalGroup("Texture")]
            public Texture2D Texture => texture;

            [GUIColor(nameof(GetGUIColor))]
            [ShowInInspector, EnableGUI, HideLabel, VerticalGroup("Texture"), HorizontalGroup("Texture/Data")]
            public TextureType Type => type;

            [GUIColor(nameof(GetGUIColor))]
            [ShowInInspector, HideLabel, VerticalGroup("Texture"), HorizontalGroup("Texture/Data")] 
            public TextureSize Size {
                get {
                    return texture.width switch {
                        8 => TextureSize._8,
                        16 => TextureSize._16,
                        32 => TextureSize._32,
                        64 => TextureSize._64,
                        128 => TextureSize._128,
                        256 => TextureSize._256,
                        512 => TextureSize._512,
                        1024 => TextureSize._1024,
                        2048 => TextureSize._2048,
                        4096 => TextureSize._4096,
                        8192 => TextureSize._8192,
                        _ => TextureSize.Unknown,
                    };
                }
                set {
                    if (value == TextureSize.Unknown) {
                        return;
                    }
                    var size = value switch {
                        TextureSize._8 => 8,
                        TextureSize._16 => 16,
                        TextureSize._32 => 32,
                        TextureSize._64 => 64,
                        TextureSize._128 => 128,
                        TextureSize._256 => 256,
                        TextureSize._512 => 512,
                        TextureSize._1024 => 1024,
                        TextureSize._2048 => 2048,
                        TextureSize._4096 => 4096,
                        TextureSize._8192 => 8192,
                        _ => -1
                    };
                    if (size == -1) {
                        return;
                    }
                    string assetPath = AssetDatabase.GetAssetPath(texture);
                    if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer) {
                        importer.maxTextureSize = size;
                        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                    }
                }
            }
            
            [GUIColor(nameof(GetGUIColor))]
            [ShowInInspector, EnableGUI, HideLabel, VerticalGroup("Texel Density"), HorizontalGroup("Texel Density/Rate")]
            public SimpleRate SizeRate {
                get {
                    var texelDensity = TexelDensity;
                    var desiredTexelDensity = SharedSettings.Instance.TexelDensity(type);
                    if (texelDensity < desiredTexelDensity.min) {
                        var differencePerc = (texelDensity - desiredTexelDensity.min) / desiredTexelDensity.min;
                        if (differencePerc < -0.5f) {
                            return SimpleRate.TooSmall;
                        } else {
                            return SimpleRate.Small;
                        }
                    } else if (texelDensity > desiredTexelDensity.max) {
                        var differencePerc = (texelDensity - desiredTexelDensity.max) / desiredTexelDensity.max;
                        if (differencePerc < 0.5f) {
                            return SimpleRate.Big;
                        } else {
                            return SimpleRate.TooBig;
                        }
                    } else {
                        return SimpleRate.Good;
                    }
                }
            }
            
            [GUIColor(nameof(GetGUIColor))]
            [ShowInInspector, EnableGUI, HideLabel, VerticalGroup("Texel Density"), HorizontalGroup("Texel Density/Rate")] 
            float TexelDensity => geometryData.texelDensityFactor * texture.width;

            bool NeedToFix => SizeRate != SimpleRate.Good;
            
            [GUIColor(nameof(GetGUIColor))]
            [Button("Fix: Low"), VerticalGroup("Texel Density"), HorizontalGroup("Texel Density/Fix"), ShowIf(nameof(NeedToFix))]
            void FixSizeLow() {
                var desiredTexelDensityRange = SharedSettings.Instance.TexelDensity(type);
                var desiredTexelDensity = (desiredTexelDensityRange.min * 3 + desiredTexelDensityRange.max) / 4;
                FixTexelDensity(desiredTexelDensity);
            }

            [GUIColor(nameof(GetGUIColor))]
            [Button("Fix: High"), VerticalGroup("Texel Density"), HorizontalGroup("Texel Density/Fix"), ShowIf(nameof(NeedToFix))]
            void FixSizeHigh() {
                var desiredTexelDensityRange = SharedSettings.Instance.TexelDensity(type);
                var desiredTexelDensity = (desiredTexelDensityRange.min + desiredTexelDensityRange.max * 3) / 4;
                FixTexelDensity(desiredTexelDensity);
            }

            void FixTexelDensity(float desiredTexelDensity) {
                var desiredSize = desiredTexelDensity / geometryData.texelDensityFactor;
                
                string assetPath = AssetDatabase.GetAssetPath(texture);
                if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer) {
                    int size = Mathf.ClosestPowerOfTwo(Mathf.FloorToInt(desiredSize));
                    importer.maxTextureSize = size;
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                }
            }

            Color GetGUIColor() {
                return SizeRate switch {
                    SimpleRate.TooSmall => GUIColors.Red,
                    SimpleRate.Small => GUIColors.Yellow,
                    SimpleRate.Good => GUIColors.White,
                    SimpleRate.Big => GUIColors.Yellow,
                    SimpleRate.TooBig => GUIColors.Red,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }
        
        [Serializable]
        class Result {
            [HideLabel, VerticalGroup("Geometry")] public MeshData meshData;
            [HideLabel, VerticalGroup("Textures"), TableList(AlwaysExpanded = true, DrawScrollView = false)] public List<TextureData> textures;
            [VerticalGroup("Textures"), ShowIf(nameof(HasUnhandledTextures))] public List<string> unhandledTextures;
            
            bool HasUnhandledTextures => unhandledTextures.Count > 0;
            
            public Result(in MeshData meshData, List<TextureData> textures, List<string> unhandledTextures) {
                this.meshData = meshData;
                this.textures = textures;
                this.unhandledTextures = unhandledTextures;

                foreach (var textureData in textures) {
                    textureData.geometryData = meshData.geometry;
                }
            }
        }

        [Serializable]
        class SharedSettings {
            public static SharedSettings Instance { get; set; }
            
            [MinMaxSlider(2.56f, 20.48f, true)] public Vector2 albedoTexelDensity = new (5.12f, 10.24f);
            [MinMaxSlider(2.56f, 20.48f, true)] public Vector2 normalTexelDensity = new (2.56f, 5.12f);
            [MinMaxSlider(2.56f, 20.48f, true)] public Vector2 maskTexelDensity = new (2.56f, 5.12f);
            [Space(10)] 
            [Range(0, 1)] public float maxUvDeviation = 0.25f;
            [Range(0, 1)] public float minTextureCoverage = 0.6f;
            
            public FloatRange TexelDensity(TextureType type) => type switch {
                TextureType.Albedo => new FloatRange(albedoTexelDensity.x, albedoTexelDensity.y),
                TextureType.Normal => new FloatRange(normalTexelDensity.x, normalTexelDensity.y),
                TextureType.Mask => new FloatRange(maskTexelDensity.x, maskTexelDensity.y),
                TextureType.Invalid => new FloatRange(0, 0),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            };
        }
        
        enum TextureType : byte {
            Albedo,
            Normal,
            Mask,
            Invalid,
        }
        
        enum TextureSize : byte {
            Unknown,
            _8,
            _16,
            _32,
            _64,
            _128,
            _256,
            _512,
            _1024,
            _2048,
            _4096,
            _8192,
        }

        enum SimpleRate : byte {
            TooSmall,
            Small,
            Good,
            Big,
            TooBig,
        }
        
        static class Id {
            public static readonly int[] Albedos = {
                Shader.PropertyToID("_MainTex"),
                Shader.PropertyToID("_BaseColorMap"),
                Shader.PropertyToID("_Base_BaseColor"),
                Shader.PropertyToID("_Overlay_BaseColor"),
                Shader.PropertyToID("_TrunkBaseColorMap"),
                Shader.PropertyToID("_BarkBaseColorMap"),
            };
            public static readonly int[] Normals = {
                Shader.PropertyToID("_NormalMap"),
                Shader.PropertyToID("_NormalMapOS"),
                Shader.PropertyToID("_BumpMap"),
                Shader.PropertyToID("_BentNormalMap"),
                Shader.PropertyToID("_BentNormalMapOS"),
                Shader.PropertyToID("_HeightMap"),
                Shader.PropertyToID("_Base_NormalMap"),
                Shader.PropertyToID("_Overlay_NormalMap"),
                Shader.PropertyToID("_TangentMap"),
                Shader.PropertyToID("_TangentMapOS"),
                Shader.PropertyToID("_TrunkNormalMap"),
                Shader.PropertyToID("_BarkNormalMap"),
            };
            public static readonly int[] MaskMaps = {
                Shader.PropertyToID("_MaskMap"),
                Shader.PropertyToID("_Mask"),
                Shader.PropertyToID("_SmoothnessMask"),
                Shader.PropertyToID("_Base_MaskMap"),
                Shader.PropertyToID("_Overlay_MaskMap"),
                Shader.PropertyToID("_EmissiveColorMap"),
                Shader.PropertyToID("_EmissionMap"),
                Shader.PropertyToID("_AnisotropyMap"),
                Shader.PropertyToID("_SubsurfaceMaskMap"),
                Shader.PropertyToID("_TransmissionMaskMap"),
                Shader.PropertyToID("_ThicknessMap"),
                Shader.PropertyToID("_IridescenceThicknessMap"),
                Shader.PropertyToID("_IridescenceMaskMap"),
                Shader.PropertyToID("_SpecularColorMap"),
                Shader.PropertyToID("_TransmittanceColorMap"),
                Shader.PropertyToID("_TrunkMaskMap"),
                Shader.PropertyToID("_BarkMaskMap"),
                Shader.PropertyToID("_TrunkBarkMap"),
            };
            public static readonly int[] Invalids = {
                Shader.PropertyToID("_DetailMap"),
                Shader.PropertyToID("_CoatMaskMap"),
            };
            public static readonly int[] ToSkip = {
                Shader.PropertyToID("unity_Lightmaps"),
                Shader.PropertyToID("unity_LightmapsInd"),
                Shader.PropertyToID("unity_ShadowMasks"),
            };
            
            public static readonly int[] All = ArrayUtils.Concat(Albedos, Normals, MaskMaps, Invalids, ToSkip);
        }
    }
}