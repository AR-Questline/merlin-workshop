using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Character {
    [UsesPrefab("CharacterSheet/Character/" + nameof(VCharacterTabs))]
    public class VCharacterTabs : View<CharacterSubTabs>, IAutoFocusBase { }
}
