using Awaken.TG.Main.Localization;
using Awaken.TG.Utility;
using Awaken.TG.Main.Saving;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Parts {
    public partial class CCHeroName : CharacterCreatorPart {
        public CharacterCreator CharacterCreator => ParentModel.ParentModel;

        public override string Title => LocTerms.CharacterCreatorName.Translate();

        public string SavedValue {
            get => Hero.Current.Name;
            set => Hero.Current.Name = value;
        }
    }
}