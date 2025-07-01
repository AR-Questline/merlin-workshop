using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using FMODUnity;

namespace Awaken.TG.Main.AudioSystem {
    public class HeroGenderSpecificFModEventRef : FModEventRef {
        public EventReference femaleEventPath;

        protected override EventReference EventPath => Hero.Current.GetGender() switch {
            Gender.Female => femaleEventPath,
            _ => eventPath
        };
    }
}