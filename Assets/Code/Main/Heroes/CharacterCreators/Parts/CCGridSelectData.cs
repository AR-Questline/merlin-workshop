using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.RawImageRendering;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Parts {
    public readonly struct CCGridSelectData {
        public readonly string Title;
        public readonly int Count;
        public readonly GridSelectType Type;
        
        public readonly Func<int> GetSavedValue;
        public readonly Action<int> SetSavedValue;

        public readonly Func<int, ShareableSpriteReference> GetSpriteOf;
        public readonly Func<int, Color> GetColorOf;
        public readonly bool OverrideViewTarget;
        public readonly HeroRenderer.Target ViewTarget;

        CCGridSelectData(string title, int count, Func<int> getSavedValue, Action<int> setSavedValue, Func<int, ShareableSpriteReference> getSpriteOf, bool overrideViewTarget = false, HeroRenderer.Target viewTarget = HeroRenderer.Target.CCBody) {
            Title = title;
            Count = count;
            Type = GridSelectType.Icon;
            GetSavedValue = getSavedValue;
            SetSavedValue = setSavedValue;
            GetSpriteOf = getSpriteOf;
            GetColorOf = null;
            OverrideViewTarget = overrideViewTarget;
            ViewTarget = viewTarget;
        }
        
        CCGridSelectData(string title, int count, Func<int> getSavedValue, Action<int> setSavedValue, Func<int, Color> getColorOf, bool overrideViewTarget = false, HeroRenderer.Target viewTarget = HeroRenderer.Target.CCBody) {
            Title = title;
            Count = count;
            Type = GridSelectType.Color;
            GetSavedValue = getSavedValue;
            SetSavedValue = setSavedValue;
            GetSpriteOf = null;
            GetColorOf = getColorOf;
            OverrideViewTarget = overrideViewTarget;
            ViewTarget = viewTarget;
        }

        public static CCGridSelectData Faces(CharacterCreator creator) {
            return new CCGridSelectData(
                LocTerms.CharacterCreatorHeadShapes.Translate(),
                creator.Template.HeadShapesCount,
                creator.GetHeadShapeIndex,
                creator.SetHeadShapeIndex,
                index => creator.Template.HeadShape(creator.GetGender(), index).Icon
            );
        }
        
        public static CCGridSelectData FacesDetails(CharacterCreator creator) {
            return new CCGridSelectData(
                LocTerms.CharacterCreatorFaceDetails.Translate(),
                creator.Template.FaceSkinCount,
                creator.GetFaceSkinIndex,
                creator.SetFaceSkinIndex,
                index => creator.Template.FaceSkin(creator.GetGender(), index).Icon
            );
        }
        
        public static CCGridSelectData Hairs(CharacterCreator creator) {
            return new CCGridSelectData(
                LocTerms.CharacterCreatorHairs.Translate(),
                creator.Template.HairsCount,
                creator.GetHairIndex,
                creator.SetHairIndex,
                index => creator.Template.Hair(creator.GetGender(), index).Icon
            );
        }
        
        public static CCGridSelectData Beards(CharacterCreator creator) {
            return new CCGridSelectData(
                LocTerms.CharacterCreatorBeard.Translate(),
                creator.Template.BeardsCount,
                creator.GetBeardIndex,
                creator.SetBeardIndex,
                index => creator.Template.Beard(creator.GetGender(), index).Icon
            );
        }
        
        public static CCGridSelectData SkinColor(CharacterCreator creator) {
            return new CCGridSelectData(
                LocTerms.CharacterCreatorSkinColor.Translate(),
                creator.Template.SkinColorsCount,
                creator.GetSkinColorIndex,
                creator.SetSkinColorIndex,
                index => creator.Template.SkinColor(index).ui
            );
        }
        
        public static CCGridSelectData HairColor(CharacterCreator creator) {
            return new CCGridSelectData(
                string.Empty,
                creator.Template.HairColorsCount,
                creator.GetHairColorIndex,
                creator.SetHairColorIndex,
                index => creator.Template.HairColor(index).ui);
        }
        
        public static CCGridSelectData BeardColor(CharacterCreator creator) {
            return new CCGridSelectData(
                string.Empty,
                creator.Template.HairColorsCount,
                creator.GetBeardColorIndex,
                creator.SetBeardColorIndex,
                index => creator.Template.HairColor(index).ui);
        }
        
        public static CCGridSelectData EyeColor(CharacterCreator creator) {
            return new CCGridSelectData(
                LocTerms.CharacterCreatorEyeColor.Translate(),
                creator.Template.EyeColorsCount,
                creator.GetEyeColorIndex,
                creator.SetEyeColorIndex,
                index => creator.Template.EyeColor(index).ui);
        }
        
        public static CCGridSelectData Eyebrow(CharacterCreator creator) {
            return new CCGridSelectData(
                LocTerms.CharacterCreatorEyebrows.Translate(),
                creator.Template.EyebrowsCount,
                creator.GetEyebrowIndex,
                creator.SetEyebrowIndex,
                index => creator.Template.Eyebrow(creator.GetGender(), index).Icon);
        }
        
        public static CCGridSelectData FaceTattoo(CharacterCreator creator) {
            return new CCGridSelectData(
                LocTerms.CharacterCreatorFaceTattoo.Translate(),
                creator.Template.FaceTattooCount,
                creator.GetFaceTattooIndex,
                creator.SetFaceTattooIndex,
                index => creator.Template.FaceTattoos(index).Icon(creator.GetGender()),
                true,
                HeroRenderer.Target.CCHead);
        }
        
        public static CCGridSelectData FaceTattooColor(CharacterCreator creator) {
            return new CCGridSelectData(
                string.Empty,
                creator.Template.TattooColorsCount,
                creator.GetFaceTattooColorIndex,
                creator.SetFaceTattooColorIndex,
                index => creator.Template.TattooColors(index).ui, 
                true,
                HeroRenderer.Target.CCHead);
        }
        
        public static CCGridSelectData BodyTattoo(CharacterCreator creator) {
            return new CCGridSelectData(
                LocTerms.CharacterCreatorBodyTattoo.Translate(),
                creator.Template.BodyTattooCount,
                creator.GetBodyTattooIndex,
                creator.SetBodyTattooIndex,
                index => creator.Template.BodyTattoos(index).Icon(creator.GetGender()),
                true);
        }
        
        public static CCGridSelectData BodyTattooColor(CharacterCreator creator) {
            return new CCGridSelectData(
                string.Empty,
                creator.Template.TattooColorsCount,
                creator.GetBodyTattooColorIndex,
                creator.SetBodyTattooColorIndex,
                index => creator.Template.TattooColors(index).ui,
                true);
        }
    }

    public enum GridSelectType : byte {
        Icon,
        Color,
    }
}