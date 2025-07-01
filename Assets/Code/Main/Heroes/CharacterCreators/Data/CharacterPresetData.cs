using System;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Character.Features.Config;
using Awaken.TG.Main.Fights.NPCs;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Data {
    [Serializable]
    public struct CharacterPresetData {
        public bool random;
        [HideIf(nameof(random))] public int gender;
        [HideIf(nameof(random))] public int skinColor;
        [HideIf(nameof(random))] public int headShape;
        [HideIf(nameof(random))] public int faceSkin;
        [HideIf(nameof(random))] public int hair;
        [HideIf(nameof(random))] public int beard;
        [HideIf(nameof(random))] public int hairColor;
        [HideIf(nameof(random))] public int beardColor;
        [HideIf(nameof(random))] public int bodyNormals;
        [HideIf(nameof(random))] public int eyeColor;
        [HideIf(nameof(random))] public int eyebrow;
        [HideIf(nameof(random))] public int bodyTattoo;
        [HideIf(nameof(random))] public int bodyTattooColor;
        [HideIf(nameof(random))] public int faceTattoo;
        [HideIf(nameof(random))] public int faceTattooColor;

        public readonly BlendShape Gender(CharacterCreatorTemplate template) => new("Female", template.FemaleShape(gender));
        public readonly ref CharacterCreatorColor SkinColor(CharacterCreatorTemplate template) => ref template.SkinColor(skinColor);
        public readonly ref CharacterHeadShapes HeadPreset(CharacterCreatorTemplate template) => ref template.HeadShape(CharacterCreatorTemplate.GenderOfIndex(gender), headShape);
        public readonly CharacterSkinConfig FaceSkin(CharacterCreatorTemplate template) => template.FaceSkin(CharacterCreatorTemplate.GenderOfIndex(gender), faceSkin);
        public readonly CharacterCloth Hair(CharacterCreatorTemplate template) => template.Hair(CharacterCreatorTemplate.GenderOfIndex(gender), hair);
        public readonly CharacterCloth Beard(CharacterCreatorTemplate template) => template.Beard(CharacterCreatorTemplate.GenderOfIndex(gender), beard);
        public readonly ref CharacterCreatorHairConfig HairColor(CharacterCreatorTemplate template) => ref template.HairColor(hairColor);
        public readonly ref CharacterCreatorHairConfig BeardColor(CharacterCreatorTemplate template) => ref template.HairColor(beardColor);
        public readonly ref CharacterBodyNormal BodyNormal(CharacterCreatorTemplate template) => ref template.BodyNormal(bodyNormals);
        public readonly ref CharacterCreatorColor EyeColor(CharacterCreatorTemplate template) => ref template.EyeColor(eyeColor);
        public readonly CharacterTexture Eyebrow(CharacterCreatorTemplate template) => template.Eyebrow(CharacterCreatorTemplate.GenderOfIndex(gender), eyebrow);
        public readonly CharacterBodyTattoo BodyTattoo(CharacterCreatorTemplate template) => template.BodyTattoos(bodyTattoo);
        public readonly ref CharacterCreatorColor BodyTattooColor(CharacterCreatorTemplate template) => ref template.TattooColors(bodyTattooColor);
        public readonly CharacterFaceTattoo FaceTattoo(CharacterCreatorTemplate template) => template.FaceTattoos(faceTattoo);
        public readonly ref CharacterCreatorColor FaceTattooColor(CharacterCreatorTemplate template) => ref template.TattooColors(faceTattooColor);
        
        public void Randomize(CharacterCreatorTemplate template) {
            gender = RandomUtil.UniformInt(0, 1);
            Gender genderType = CharacterCreatorTemplate.GenderOfIndex(gender);
            
            skinColor = RandomUtil.UniformInt(0, template.SkinColorsCount - 1);
            headShape = RandomUtil.UniformInt(0, template.CalculateHeadShapesCount(genderType) - 1);
            faceSkin = RandomUtil.UniformInt(0, template.FaceSkinCount - 1);
            hair = RandomUtil.UniformInt(0, template.CalculateHairsCount(genderType) - 1);
            beard = RandomUtil.UniformInt(0, template.CalculateBeardsCount(genderType) - 1);
            hairColor = RandomUtil.UniformInt(0, template.HairColorsCount - 1);
            beardColor = RandomUtil.UniformInt(0, template.HairColorsCount - 1);
            bodyNormals = RandomUtil.UniformInt(0, template.BodyNormalsCount - 1);
            eyeColor = RandomUtil.UniformInt(0, template.EyeColorsCount - 1);
            eyebrow = RandomUtil.UniformInt(0, template.CalculateEyebrowsCount(genderType) - 1);
            bodyTattoo = RandomUtil.UniformInt(0, template.BodyTattooCount - 1);
            bodyTattooColor = RandomUtil.UniformInt(0, template.TattooColorsCount - 1);
            faceTattoo = RandomUtil.UniformInt(0, template.FaceTattooCount - 1);
            faceTattooColor = RandomUtil.UniformInt(0, template.TattooColorsCount - 1);
        }
    }
}