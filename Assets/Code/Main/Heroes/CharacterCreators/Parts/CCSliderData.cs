using System;
using Awaken.TG.Main.Heroes.CharacterCreators.Data;
using Awaken.TG.Main.Localization;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Parts {
    public readonly struct CCSliderData {
        public readonly string title;
        public readonly int minValue;
        public readonly int maxValue;
        public readonly bool cycle;
        
        public readonly Func<int> getSavedValue;
        public readonly Action<int> setSavedValue;
        
        public readonly Func<int, string> getNameOf;
        public readonly Func<int, string> getDescription;
        
        CCSliderData(string title, int minValue, int maxValue, bool cycle, Func<int> getSavedValue, Action<int> setSavedValue, Func<int, string> genNameOf, Func<int, string> getDescription = null) {
            this.title = title;
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.cycle = cycle;
            this.getSavedValue = getSavedValue;
            this.setSavedValue = setSavedValue;
            getNameOf = genNameOf;
            this.getDescription = getDescription;
        }

        public static CCSliderData Gender(CharacterCreator creator) {
            return new CCSliderData(
                LocTerms.CharacterCreatorGender.Translate(),
                0, 1, true,
                creator.GetGenderIndex,
                creator.SetGenderIndex,
                GenderName(creator)
            );
        }

        public static CCSliderData Normals(CharacterCreator creator) {
            return new CCSliderData(
                LocTerms.CharacterCreatorBodyNormals.Translate(),
                0, creator.Template.BodyNormalsCount - 1, true,
                creator.GetBodyNormalsIndex,
                creator.SetBodyNormalsIndex,
                i => creator.Template.BodyNormal(i).label
            );
        }

        [UnityEngine.Scripting.Preserve] 
        static string DefaultName(int index) {
            return (index + 1).ToString();
        }
        
        static Func<int, string> GenderName(CharacterCreator creator) {
            return index => CharacterCreatorTemplate.GenderOfIndex(index) switch {
                Fights.NPCs.Gender.Male => LocTerms.CharacterCreatorMale.Translate(),
                Fights.NPCs.Gender.Female => LocTerms.CharacterCreatorFemale.Translate(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}