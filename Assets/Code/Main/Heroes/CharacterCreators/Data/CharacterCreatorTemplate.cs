using System;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Character.Features.Config;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Data {
    public class CharacterCreatorTemplate : ScriptableObject, ITemplate {
        public string GUID { get; set; }
        
        [SerializeField, HideInInspector] TemplateMetadata metadata;
        public TemplateMetadata Metadata => metadata;

        [Title("Presets")]
        [SerializeField, TableList(CellPadding = 10, ShowIndexLabels = true)] CharacterPreset[] presets = Array.Empty<CharacterPreset>();
        [Title("Body")]
        [SerializeField] CharacterCreatorColor[] skinColors = Array.Empty<CharacterCreatorColor>();
        [SerializeField, TableList(CellPadding = 10, ShowIndexLabels = true)] CharacterBodyNormal[] bodyNormals = Array.Empty<CharacterBodyNormal>();
        [Title("Head")]
        [SerializeField, TableList(CellPadding = 10, ShowIndexLabels = true)] HeadShapesRow[] headShapes = Array.Empty<HeadShapesRow>();
        [SerializeField, TableList(CellPadding = 10, ShowIndexLabels = true)] CharacterSkinTextureRow[] faceSkin = Array.Empty<CharacterSkinTextureRow>();
        [SerializeField, TableList(CellPadding = 10, ShowIndexLabels = true)] CharacterMeshRow[] hairs = Array.Empty<CharacterMeshRow>();
        [SerializeField, TableList(CellPadding = 10, ShowIndexLabels = true)] CharacterMeshRow[] beards = Array.Empty<CharacterMeshRow>();
        [SerializeField] CharacterCreatorHairConfig[] hairColors = Array.Empty<CharacterCreatorHairConfig>();
        [SerializeField] CharacterCreatorColor[] eyeColors = Array.Empty<CharacterCreatorColor>();
        [SerializeField, TableList(CellPadding = 10, ShowIndexLabels = true)] CharacterTextureRow[] eyebrows = Array.Empty<CharacterTextureRow>();
        [Title("Tattoo")]
        [SerializeField] CharacterFaceTattoo[] faceTattoos = Array.Empty<CharacterFaceTattoo>();
        [SerializeField] CharacterBodyTattoo[] bodyTattoos = Array.Empty<CharacterBodyTattoo>();
        [SerializeField] CharacterCreatorColor[] tattooColors = Array.Empty<CharacterCreatorColor>();

        public int SkinColorsCount => skinColors.Length;
        public int HeadShapesCount => headShapes.Length;
        public int FaceSkinCount => faceSkin.Length;
        public int HairsCount => hairs.Length;
        public int BeardsCount => beards.Length;
        public int HairColorsCount => hairColors.Length;
        public int BodyNormalsCount => bodyNormals.Length;
        public int PresetsCount => presets.Length;
        public int EyeColorsCount => eyeColors.Length;
        public int EyebrowsCount => eyebrows.Length;
        public int FaceTattooCount => faceTattoos.Length;
        public int TattooColorsCount => tattooColors.Length;
        public int BodyTattooCount => bodyTattoos.Length;
        
        public int CalculateHeadShapesCount(Gender gender) => headShapes.Count(row => gender == Gender.Male ? row.male.Icon.IsSet : row.female.Icon.IsSet);
        public int CalculateHairsCount(Gender gender) => hairs.Count(row => gender == Gender.Male ? row.iconMale.IsSet : row.iconFemale.IsSet);
        public int CalculateBeardsCount(Gender gender) => beards.Count(row => gender == Gender.Male ? row.iconMale.IsSet : row.iconFemale.IsSet);
        public int CalculateEyebrowsCount(Gender gender) => eyebrows.Count(row => gender == Gender.Male ? row.iconMale.IsSet : row.iconFemale.IsSet);
        
        public float FemaleShape(int index) => GenderOfIndex(index) is Gender.Female ? 1f : 0f;
        public ref CharacterCreatorColor SkinColor(int index) => ref skinColors[index];
        public ref CharacterHeadShapes HeadShape(Gender gender, int index) => ref Get(ref headShapes[index], gender);
        public CharacterSkinConfig FaceSkin(Gender gender, int index) => Get(ref faceSkin[index], gender);
        public CharacterCloth Hair(Gender gender, int index) => Get(ref hairs[index], gender);
        public CharacterCloth Beard(Gender gender, int index) => Get(ref beards[index], gender);
        public ref CharacterCreatorHairConfig HairColor(int index) => ref hairColors[index];
        public ref CharacterBodyNormal BodyNormal(int index) => ref bodyNormals[index];
        public ref CharacterPreset Preset(int index) => ref presets[index];
        public ref CharacterCreatorColor EyeColor(int index) => ref eyeColors[index];
        public CharacterTexture Eyebrow(Gender gender, int index) => Get(ref eyebrows[index], gender);
        public ref CharacterFaceTattoo FaceTattoos(int index) => ref faceTattoos[index];
        public ref CharacterBodyTattoo BodyTattoos(int index) => ref bodyTattoos[index]; 
        public ref CharacterCreatorColor TattooColors(int index) => ref tattooColors[index];
        
        public static Gender GenderOfIndex(int index) {
            return index switch {
                0 => Gender.Male,
                1 => Gender.Female,
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, null)
            };
        }

        [Serializable]
        struct HeadShapesRow {
            [HideLabel, VerticalGroup("Male")] public CharacterHeadShapes male;
            [HideLabel, VerticalGroup("Female")] public CharacterHeadShapes female;
        }

        [Serializable]
        struct CharacterMeshRow {
            [UIAssetReference(AddressableLabels.UI.CharacterCreator), HideLabel, VerticalGroup("Male")] public ShareableSpriteReference iconMale;
            [UIAssetReference(AddressableLabels.UI.CharacterCreator), HideLabel, VerticalGroup("Female")] public ShareableSpriteReference iconFemale;
            [PrefabAssetReference(AddressableGroup.NPCs), HideLabel, VerticalGroup("Male")] public ARAssetReference male;
            [PrefabAssetReference(AddressableGroup.NPCs), HideLabel, VerticalGroup("Female")] public ARAssetReference female;
        }
        
        [Serializable]
        struct CharacterTextureRow {
            [UIAssetReference(AddressableLabels.UI.CharacterCreator), HideLabel, VerticalGroup("Male")] public ShareableSpriteReference iconMale;
            [UIAssetReference(AddressableLabels.UI.CharacterCreator), HideLabel, VerticalGroup("Female")] public ShareableSpriteReference iconFemale;
            [TextureAssetReference, HideLabel, VerticalGroup("Male")] public ShareableARAssetReference male;
            [TextureAssetReference, HideLabel, VerticalGroup("Female")] public ShareableARAssetReference female;
        }
        
        [Serializable]
        struct CharacterSkinTextureRow {
            [UIAssetReference(AddressableLabels.UI.CharacterCreator), HideLabel, VerticalGroup("Male")] public ShareableSpriteReference iconMale;
            [UIAssetReference(AddressableLabels.UI.CharacterCreator), HideLabel, VerticalGroup("Female")] public ShareableSpriteReference iconFemale;
            [HideLabel, VerticalGroup("Male"), InlineProperty] public CharacterSkinConfig male;
            [HideLabel, VerticalGroup("Female"), InlineProperty] public CharacterSkinConfig female;
        }

        static ref CharacterHeadShapes Get(ref HeadShapesRow row, Gender gender) {
            switch (gender) {
                case Gender.Male: return ref row.male;
                case Gender.Female: return ref row.female;
                default: throw new ArgumentOutOfRangeException(nameof(gender), gender, null);
            }
        }

        static CharacterCloth Get(ref CharacterMeshRow row, Gender gender) {
            return gender switch {
                Gender.Male => new CharacterCloth(row.iconMale, row.male),
                Gender.Female => new CharacterCloth(row.iconFemale, row.female),
                _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, null)
            };
        } 
        
        static CharacterTexture Get(ref CharacterTextureRow row, Gender gender) {
            return gender switch {
                Gender.Male => new CharacterTexture(row.iconMale, row.male),
                Gender.Female => new CharacterTexture(row.iconFemale, row.female),
                _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, null)
            };
        }
        
        static CharacterSkinConfig Get(ref CharacterSkinTextureRow row, Gender gender) {
            return gender switch {
                Gender.Male => new CharacterSkinConfig(row.iconMale, row.male),
                Gender.Female => new CharacterSkinConfig(row.iconFemale, row.female),
                _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, null)
            };
        }

        string INamed.DisplayName => string.Empty;
        string INamed.DebugName => name;
    }
}