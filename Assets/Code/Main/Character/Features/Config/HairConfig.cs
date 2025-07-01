using System;
using Awaken.TG.Main.Templates;
using Awaken.TG.Utility.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features.Config {
    [CreateAssetMenu(menuName = "NpcData/BodyFeatures/Hair")]
    public class HairConfig : ScriptableObject, ITemplate {
        [InlineProperty, HideLabel] public CharacterHairColor data;

        [field: SerializeField] public string GUID { get; set; }
        public string DisplayName => string.Empty;
        public string DebugName => name;
        public TemplateMetadata Metadata => null;

#if UNITY_EDITOR
        void OnEnable() {
            if (string.IsNullOrWhiteSpace(GUID) && UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(this, out var guid, out _)) {
                GUID = guid;
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
            }
        }
#endif
    }
    
    [Serializable]
    public struct CharacterCreatorHairConfig {
        public Color ui;
        public HairConfig config;
    }
    
    [Serializable]
    public partial struct CharacterHairColor {
        const float LabelWidth = 160;
        
        static readonly int HairColorID = Shader.PropertyToID("_DiffuseColor");
        static readonly int ScalpColorID = Shader.PropertyToID("_BaseColor");
        static readonly int DiffuseStrengthID = Shader.PropertyToID("_DiffuseStrength");
        
        static readonly int VertexBaseColorID = Shader.PropertyToID("_VertexBaseColor");
        static readonly int VertexColorStrengthID = Shader.PropertyToID("_VertexColorStrength");
        
        static readonly int SpecularTintColorID = Shader.PropertyToID("_SpecularTint");
        static readonly int RootColorID = Shader.PropertyToID("_RootColor");
        static readonly int EndColorID = Shader.PropertyToID("_EndColor");
        
        static readonly int HighlightAColorID = Shader.PropertyToID("_HighlightAColor");
        static readonly int HighlightBColorID = Shader.PropertyToID("_HighlightBColor");
        
        [LabelText("Legacy Tint"), HideIf("@" + nameof(diffuseColor) + ".active")]
        [SerializeField, LabelWidth(LabelWidth)] public Color tint;
        [InlineProperty, LabelWidth(LabelWidth)] public ShaderProperty<Color> diffuseColor;
        [InlineProperty, LabelWidth(LabelWidth)] public ShaderProperty<float> diffuseStrength;
        [InlineProperty, LabelWidth(LabelWidth)] public ShaderProperty<Color> scalpColor;
        [Space]
        [InlineProperty, LabelWidth(LabelWidth)] public ShaderProperty<Color> vertexBaseColor;
        [InlineProperty, LabelWidth(LabelWidth)] public ShaderProperty<float> vertexColorStrength;
        [Space]
        [InlineProperty, LabelWidth(LabelWidth)] public ShaderProperty<Color> specularTintColor;
        [InlineProperty, LabelWidth(LabelWidth)] public ShaderProperty<Color> rootColor;
        [InlineProperty, LabelWidth(LabelWidth)] public ShaderProperty<Color> endColor;
        [Space]
        [InlineProperty, LabelWidth(LabelWidth)] public ShaderProperty<Color> highlightAColor;
        [InlineProperty, LabelWidth(LabelWidth)] public ShaderProperty<Color> highlightBColor;
        
        [Title("Beard")]
        [InlineProperty, LabelWidth(LabelWidth)] [UnityEngine.Scripting.Preserve] public ShaderProperty<Color> beardDiffuseColor;
        [InlineProperty, LabelWidth(LabelWidth)] public ShaderProperty<float> beardDiffuseStrength;
        [InlineProperty, LabelWidth(LabelWidth)] public ShaderProperty<Color> beardScalpColor;
        [Space]
        [InlineProperty, LabelWidth(LabelWidth)] public ShaderProperty<Color> beardVertexBaseColor;
        [InlineProperty, LabelWidth(LabelWidth)] public ShaderProperty<float> beardVertexColorStrength;
        [Space]
        [InlineProperty, LabelWidth(LabelWidth)] public ShaderProperty<Color> beardSpecularTintColor;
        [InlineProperty, LabelWidth(LabelWidth)] public ShaderProperty<Color> beardRootColor;
        [InlineProperty, LabelWidth(LabelWidth)] public ShaderProperty<Color> beardEndColor;
        [Space]
        [InlineProperty, LabelWidth(LabelWidth)] public ShaderProperty<Color> beardHighlightAColor;
        [InlineProperty, LabelWidth(LabelWidth)] public ShaderProperty<Color> beardHighlightBColor;

        [Button]
        void CopyPropertiesToBeardProperties() {
            beardDiffuseColor = diffuseColor;
            beardDiffuseStrength = diffuseStrength;
            beardScalpColor = scalpColor;
            beardVertexBaseColor = vertexBaseColor;
            beardVertexColorStrength = vertexColorStrength;
            beardSpecularTintColor = specularTintColor;
            beardRootColor = rootColor;
            beardEndColor = endColor;
            beardHighlightAColor = highlightAColor;
            beardHighlightBColor = highlightBColor;
        }
        
        public readonly void ApplyTo(Material hairMaterial, bool isBeard = false) {
            if (isBeard) {
                ApplyToBeard(hairMaterial);
                return;
            }
            
            hairMaterial.SetColor(HairColorID, diffuseColor.active ? diffuseColor.value : tint);
            
            if (diffuseStrength.active)     hairMaterial.SetFloat(DiffuseStrengthID, diffuseStrength.value);
            if (scalpColor.active)          hairMaterial.SetColor(ScalpColorID, scalpColor.value);
            
            if (vertexBaseColor.active)     hairMaterial.SetColor(VertexBaseColorID, vertexBaseColor.value);
            if (vertexColorStrength.active) hairMaterial.SetFloat(VertexColorStrengthID, vertexColorStrength.value);

            if (specularTintColor.active)   hairMaterial.SetColor(SpecularTintColorID, specularTintColor.value);
            if (rootColor.active)           hairMaterial.SetColor(RootColorID, rootColor.value);
            if (endColor.active)            hairMaterial.SetColor(EndColorID, endColor.value);
            
            if (highlightAColor.active)     hairMaterial.SetColor(HighlightAColorID, highlightAColor.value);
            if (highlightBColor.active)     hairMaterial.SetColor(HighlightBColorID, highlightBColor.value);
        }

        public readonly void ApplyToBeard(Material hairMaterial) {
            hairMaterial.SetColor(HairColorID, diffuseColor.active ? diffuseColor.value : tint);
            
            if (beardDiffuseStrength.active)     hairMaterial.SetFloat(DiffuseStrengthID, beardDiffuseStrength.value);
            if (beardScalpColor.active)          hairMaterial.SetColor(ScalpColorID, beardScalpColor.value);
            
            if (beardVertexBaseColor.active)     hairMaterial.SetColor(VertexBaseColorID, beardVertexBaseColor.value);
            if (beardVertexColorStrength.active) hairMaterial.SetFloat(VertexColorStrengthID, beardVertexColorStrength.value);

            if (beardSpecularTintColor.active)   hairMaterial.SetColor(SpecularTintColorID, beardSpecularTintColor.value);
            if (beardRootColor.active)           hairMaterial.SetColor(RootColorID, beardRootColor.value);
            if (beardEndColor.active)            hairMaterial.SetColor(EndColorID, beardEndColor.value);
            
            if (beardHighlightAColor.active)     hairMaterial.SetColor(HighlightAColorID, beardHighlightAColor.value);
            if (beardHighlightBColor.active)     hairMaterial.SetColor(HighlightBColorID, beardHighlightBColor.value);
        }

        [Serializable]
        public partial struct ShaderProperty<T> where T : unmanaged {
            [HorizontalGroup("Pair", width: 20), HideLabel]
            public bool active;
            [HorizontalGroup("Pair"), HideLabel]
            [EnableIf(nameof(active))]
            public T value;
        }
    }
}