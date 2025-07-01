using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map {
    [UsesPrefab("CharacterSheet/Map/" + nameof(VMapSubTabsUI))]
    public class VMapSubTabsUI : View<MapSubTabsUI> {
        [SerializeField] Transform buttonHost;
        [SerializeField] GameObject buttonPrefab;
        
        protected override void OnInitialize() {
            foreach (var type in Target.Types) {
                Instantiate(buttonPrefab, buttonHost).GetComponent<VCMapSubTabButton>().Setup(type);
            }
        }
    }
}