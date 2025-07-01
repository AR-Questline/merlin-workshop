using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.EmptyContent;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Bag {
    [UsesPrefab("CharacterSheet/Bag/" + nameof(VBagUI))]
    public class VBagUI : View<BagUI>, IEmptyInfo {
        [SerializeField] Transform itemsHost;
        [SerializeField] EventReference dropHoldSound;
        
        [Title("Empty Info")]
        [SerializeField] CanvasGroup contentGroup;
        [SerializeField] VCEmptyInfo emptyInfo;

        public Transform ItemsHost => itemsHost;
        public EventReference DropHoldSound => dropHoldSound;
        public CanvasGroup[] ContentGroups => new[] { contentGroup };
        public VCEmptyInfo EmptyInfoView => emptyInfo;

        protected override void OnInitialize() {
            PrepareEmptyInfo();
        }
        
        public void PrepareEmptyInfo() {
            emptyInfo.Setup(ContentGroups, LocTerms.EmptyBagInfo.Translate(), LocTerms.EmptyBagDesc.Translate());
        }
    }
}