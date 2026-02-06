using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility.GameObjects;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.WyrdArthur {
    [UsesPrefab("CharacterSheet/WyrdArthur/" + nameof(VWyrdArthurUI))]
    public class VWyrdArthurUI : View<WyrdArthurUI>, IAutoFocusBase {
        [SerializeField] Transform powerTalentHost;
        [SerializeField] TMP_Text requiredInfo;
        
        public Transform PowerTalentHost => powerTalentHost;
        
        protected override void OnMount() {
            requiredInfo.text = LocTerms.UITalentFireplaceRequired.Translate();
            requiredInfo.TrySetActiveOptimized(!TalentTree.IsUpgradeAvailable);
        }
    }
}