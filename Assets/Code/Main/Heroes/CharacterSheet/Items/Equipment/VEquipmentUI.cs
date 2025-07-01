using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Loadouts;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.Cameras;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Equipment {
    public class VEquipmentUI : View<LoadoutsUI> {
        const float FadeDuration = 0.25f;
        const float PartFadeValue = 0.4f;
        
        [Title("General")]
        [field: SerializeField] public Component DefaultFocus { [UnityEngine.Scripting.Preserve] get; private set; }
        [SerializeField] CanvasGroup quickAndAutoSlotsCanvasGroup;
        [SerializeField] CanvasGroup armorSlotsCanvasGroup;
        [SerializeField] CanvasGroup armorWeightCanvasGroup;
        [SerializeField] CanvasGroup armorWeightInfoCanvasGroup;
        
        [SerializeField] RectTransform armorWeightTarget;
        [SerializeField] Transform armorWeightChooseHost;
        [SerializeField] Transform armorWeightEqHost;

        [Title("Section titles")]
        [SerializeField] TMP_Text armorTitle;
        [SerializeField] TMP_Text quickSlotsTitle;
        [SerializeField] TMP_Text autoSlotsTitle;

        [Title("Weight description")]
        [SerializeField] TMP_Text weightLabel;
        [SerializeField] TMP_Text weightValue;
        [SerializeField] TMP_Text weightTyeText;
        [SerializeField] TMP_Text weightEffectDescription;
        
        [Title("Weight bar")]
        [SerializeField] Image armorBar;
        [SerializeField] Image barMediumSeparator;
        [SerializeField] Image barHeavySeparator;
        
        Tween _weightTween;
        Tween _weightInfoTween;
        Tween _slotsTween;
        Tween _quickSlotsTween;

        protected override void OnInitialize() {
            weightLabel.text = LocTerms.UILoadoutArmorWeight.Translate();

            var armorWeight = Target.HeroItems.ParentModel.Element<ArmorWeight>();
            armorWeight.GetDescription();
            armorWeight.ListenTo(ArmorWeight.Events.ArmorWeightScoreChanged, SetArmorWeightBar, this);
            SetArmorWeightBar(armorWeight.ArmorWeightScore);
            
            barMediumSeparator.rectTransform.anchorMin = new Vector2(GameConstants.Get.LightArmorThreshold / GameConstants.Get.HeavyArmorThreshold, 0);
            barMediumSeparator.rectTransform.anchorMax = new Vector2(GameConstants.Get.LightArmorThreshold / GameConstants.Get.HeavyArmorThreshold, 1);
            barHeavySeparator.rectTransform.anchorMin = new Vector2(GameConstants.Get.MediumArmorThreshold / GameConstants.Get.HeavyArmorThreshold, 0);
            barHeavySeparator.rectTransform.anchorMax = new Vector2(GameConstants.Get.MediumArmorThreshold / GameConstants.Get.HeavyArmorThreshold, 1);
            
            quickSlotsTitle.text = LocTerms.UILoadoutQuickSlotsTitle.Translate();
            autoSlotsTitle.text = LocTerms.UILoadoutAutoFillSlotsTitle.Translate();
        }

        public void SetVisible(bool chooseVisible, bool dimArmor, bool dimQuickAndAutoSlots) {
            KillTweens();
            
            float weightTarget = dimQuickAndAutoSlots ? 0f : dimArmor ? PartFadeValue : 1f;
            _weightTween = armorWeightCanvasGroup.DOFade(weightTarget,FadeDuration).SetUpdate(true);
            float slotsTarget = dimQuickAndAutoSlots ? 0f : dimArmor ? PartFadeValue : chooseVisible ? 0f : 1f;
            _slotsTween = armorSlotsCanvasGroup.DOFade(slotsTarget, FadeDuration).SetUpdate(true);
            _weightInfoTween = armorWeightInfoCanvasGroup.DOFade(slotsTarget, FadeDuration).SetUpdate(true);
            float quickSlotTarget = !chooseVisible ? 1f : dimQuickAndAutoSlots ? PartFadeValue : 0f;
            _quickSlotsTween = quickAndAutoSlotsCanvasGroup.DOFade(quickSlotTarget, FadeDuration).SetUpdate(true);
            
            if (weightTarget > 0f) {
                armorWeightTarget.StretchToParent(chooseVisible && !dimArmor ? armorWeightChooseHost : armorWeightEqHost);
                armorWeightTarget.gameObject.SetActive(true);
            } else {
                armorWeightTarget.gameObject.SetActive(false);
            }
        }

        void SetArmorWeightBar(float fillAmount) {
            var armorWeight = Target.HeroItems.ParentModel.Element<ArmorWeight>();
            weightValue.text = $"{armorWeight.EquipmentWeight:F1}/{armorWeight.MaxEquipmentWeight:F1} {LocTerms.KilogramAbbreviation.Translate()}";
            var armorWeightName = armorWeight.ArmorWeightType.DisplayName.ToString();
            weightTyeText.color = ARColor.MainAccent;
            
            weightTyeText.text = armorWeightName.ToLower() switch {
                "light" => $"{armorWeightName.FontLight()}",
                "medium" => $"{armorWeightName.FontRegular()}",
                "heavy" => $"{armorWeightName.FontBold()}",
                "overload" => $"{armorWeightName.FontBold()}",
                _ => armorWeightName
            };
            
            float armor = Hero.Current.ArmorValue() * 0.01f;
            armorTitle.text = $"{LocTerms.Armor.Translate()} {armor:0.#%}";
            
            weightEffectDescription.text = armorWeight.CreateDescription();
            armorBar.fillAmount = fillAmount / GameConstants.Get.HeavyArmorThreshold;
        }

        void KillTweens() {
            _weightTween.Kill();
            _slotsTween.Kill();
            _quickSlotsTween.Kill();
            _weightInfoTween.Kill();
        }
        
        protected override IBackgroundTask OnDiscard() {
            KillTweens();
            return base.OnDiscard();
        }
    }
}