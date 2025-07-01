using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Gems.GemManagement {
    [UsesPrefab("Gems/" + nameof(VGemManagementUI))]
    public class VGemManagementUI : VGemBaseUI, IUIAware {
        const float FadeLeftDuration = 0.25f;
        [SerializeField] CanvasGroup leftCanvasGroup;
        [SerializeField] GameObject costParent;
        
        [field: SerializeField] public Transform ChooseHost { get; private set; }
        
        public void UpdateCostValue(int cost, bool isVisible) {
            costParent.SetActive(isVisible);
        }

        public void FadeLeftSide(float targetAlpha) {
            leftCanvasGroup.DOFade(targetAlpha, FadeLeftDuration).SetUpdate(true);
        }

        public UIResult Handle(UIEvent evt) {
            if (Target.TryGetElement<GemChooseUI>(out var equipmentChooseUI)) {
                if (evt is UIEPointTo) {
                    return UIResult.Accept;
                }

                if (evt is ISubmit) {
                    var items = equipmentChooseUI.TryGetElement<ItemsUI>();
                    if (items is {HoveredItem: null}) {
                        Target.Element<GemChooseUI>().Discard();
                        return UIResult.Accept;
                    }
                }
            }
            
            return UIResult.Ignore;
        }
    }
}