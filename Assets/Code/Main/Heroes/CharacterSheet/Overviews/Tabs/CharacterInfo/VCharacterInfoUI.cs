using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterInfo {
    [UsesPrefab("CharacterSheet/Overview/VCharacterInfoUI")]
    public class VCharacterInfoUI : View<CharacterInfoUI>, IAutoFocusBase {
        [SerializeField] Transform proficienciesParent;
        [SerializeField] Transform activeEffectsParent;
        
        public Transform ProficienciesParent => proficienciesParent;
        public Transform ActiveEffectsParent => activeEffectsParent;
    }
}