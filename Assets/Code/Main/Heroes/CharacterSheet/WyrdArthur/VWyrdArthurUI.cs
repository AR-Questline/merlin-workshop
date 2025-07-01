using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.WyrdArthur {
    [UsesPrefab("CharacterSheet/WyrdArthur/" + nameof(VWyrdArthurUI))]
    public class VWyrdArthurUI : View<WyrdArthurUI>, IAutoFocusBase {
        [SerializeField] Transform powerTalentHost;
        
        public Transform PowerTalentHost => powerTalentHost;
    }
}