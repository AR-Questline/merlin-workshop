using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Equipment;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.EmptyContent;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Loadouts {
    [UsesPrefab("CharacterSheet/Equipment/" + nameof(VLoadoutsUI))]
    public class VLoadoutsUI : View<LoadoutsUI>, IAutoFocusBase, IFocusSource, IUIAware, IEmptyInfo {
        const float FadeDuration = 0.25f;

        [SerializeField] Transform descriptionHost;
        [SerializeField] Transform compareDescriptionHost;
        [SerializeField] CanvasGroup loadoutsCanvasGroup;
        
        [Title("Choose List Hosts")]
        [SerializeField] Transform armorChooseHost;
        [SerializeField] Transform jewelryChooseHost;
        [SerializeField] Transform leftChooseHost;
        
        [Title("Labels")]
        [SerializeField] TMP_Text loadoutWeaponTitle;
        
        [Title("Weapon and Quick Slot Empty Info")]
        [SerializeField] CanvasGroup weaponContentGroup;
        [SerializeField] VCEmptyInfo weaponEmptyInfo;
        
        [Title("Armor Empty Info")]
        [SerializeField] CanvasGroup armorContentGroup;
        [SerializeField] CanvasGroup armorWeightContentGroup;
        [SerializeField] VCEmptyInfo armorEmptyInfo;
        
        public Transform ArmorChooseHost => armorChooseHost;
        public Transform JewelryChooseHost => jewelryChooseHost;
        public Transform LeftChooseHost => leftChooseHost;
        public CanvasGroup[] WeaponContentGroup => new[] { weaponContentGroup };
        public VCEmptyInfo WeaponEmptyInfo => weaponEmptyInfo;
        public CanvasGroup[] ContentGroups => new[] { armorContentGroup, armorWeightContentGroup };
        public VCEmptyInfo EmptyInfoView => armorEmptyInfo;

        VCLoadout _changedLoadout;
        VCLoadout[] _loadoutCollection;
        VCLoadoutSlot[] _loadoutSlotCollection;
        
        public bool ForceFocus => true;
        public Component DefaultFocus => _loadoutSlotCollection.FirstOrAny(slot => slot.LoadoutIndex == Hero.Current.HeroItems.CurrentLoadoutIndex);
        [UnityEngine.Scripting.Preserve] public VCLoadout GetLoadout(int index) => 
            _loadoutCollection.FirstOrDefault(l => l.LoadoutIndex == index);
        
        protected override void OnInitialize() {
            _loadoutCollection = GetComponentsInChildren<VCLoadout>();
            _loadoutSlotCollection = GetComponentsInChildren<VCLoadoutSlot>();

            loadoutWeaponTitle.text = LocTerms.UILoadoutWeaponTitle.Translate();
        }

        public void RefreshLoadoutNewThings() {
            foreach (var loadoutSlot in _loadoutSlotCollection) {
                loadoutSlot.RefreshNewThingsContainer();
            }
        }
        
        public void PrepareEmptyInfo() {
            weaponEmptyInfo.Setup(WeaponContentGroup);
            armorEmptyInfo.Setup(ContentGroups);
        }

        public UIResult Handle(UIEvent evt) {
            if (Target.TryGetElement<EquipmentChooseUI>(out var equipmentChooseUI)) {
                if (evt is UIEPointTo) {
                    return UIResult.Accept;
                } else if (evt is ISubmit) {
                    var items = equipmentChooseUI.TryGetElement<ItemsUI>();
                    if (items is {HoveredItem: null}) {
                        Target.Element<EquipmentChooseUI>().Discard();
                        return UIResult.Accept;
                    }
                }
            }
            
            return UIResult.Ignore;
        }

        public void FadeLoadouts(float targetAlpha) {
            loadoutsCanvasGroup.DOFade(targetAlpha, FadeDuration).SetUpdate(true);
        }
    }
}