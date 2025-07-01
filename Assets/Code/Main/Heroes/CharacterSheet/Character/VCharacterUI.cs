using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Character {
    [UsesPrefab("CharacterSheet/Character/" + nameof(VCharacterUI))]
    public class VCharacterUI : VTabParent<CharacterUI>, IAutoFocusBase  {
        protected override void OnMount() {
            ToggleTabAndContent(true);
        }
    }
}