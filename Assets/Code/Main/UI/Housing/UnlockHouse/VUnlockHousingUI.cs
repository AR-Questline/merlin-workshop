using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Helpers;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.GameObjects;
using ChocDino.UIFX;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Housing.UnlockHouse {
    [UsesPrefab("UI/Housing/" + nameof(VUnlockHousingUI))]
    public class VUnlockHousingUI : View<UnlockHousingUI>, IAutoFocusBase, IUIAware {
        const float ContentFadeDuration = 0.2f;
        const float TargetSizeGlow = 40f;
        const float GlowFadeDuration = 0.4f;
        
        [SerializeField] Image houseImage;
        [SerializeField] Image congratulationsImage;
        [SerializeField] TextMeshProUGUI houseNameText;
        [SerializeField] TextMeshProUGUI houseDescriptionText;
        [SerializeField] TextMeshProUGUI congratulationsText;
        [SerializeField] TextMeshProUGUI buyingSuccessText;
        [SerializeField] CanvasGroup buyContentCanvasGroup;
        [SerializeField] CanvasGroup successContentCanvasGroup;
        [SerializeField] OutlineFilter glowOutlineFilter;
        
        [field: SerializeField] public Transform PromptsHost { get; private set; }

        bool _isBought;
        Sequence _sequence;
        BlurBackground _buyBlurBackground;
        BlurBackground _successBlurBackground;
        BlurConfig _blurConfig = new() {
            delayFrames = 2,
            useBlurVolume = true,
        };
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        public bool IsValid => this.IsValidForUIHandle();

        protected override void OnInitialize() {
            glowOutlineFilter.Size = 0f;
            successContentCanvasGroup.alpha = 0f;
            
            string houseName = Target.unlockHousingData.HouseName;
            houseNameText.SetText(houseName);
            houseDescriptionText.SetText(Target.unlockHousingData.HouseDescription);
            congratulationsText.SetText(LocTerms.Congratulations.Translate());
            buyingSuccessText.SetText(LocTerms.HousingBuySuccess.Translate(houseName));
            
            var houseSpriteRef = Target.unlockHousingData.HouseSpriteReference;
            houseSpriteRef.RegisterAndSetup(this, houseImage);
            houseSpriteRef.RegisterAndSetup(this, congratulationsImage);
        }

        protected override void OnMount() {
            _buyBlurBackground = World.Add(new BlurBackground(Target, _blurConfig));
            _buyBlurBackground.ShowBackground(this);
        }

        public void HandleBuyingSuccess() {
            _isBought = true;
            successContentCanvasGroup.TrySetActiveOptimized(true);
            buyContentCanvasGroup.TrySetActiveOptimized(false);
            _successBlurBackground = World.Add(new BlurBackground(Target, _blurConfig));
            _successBlurBackground.ShowBackground(this);
            _buyBlurBackground.HideBackground();
            _sequence = DOTween.Sequence().SetUpdate(true)
                .Join(buyContentCanvasGroup.DOFade(0f, ContentFadeDuration))
                .Join(successContentCanvasGroup.DOFade(1f, ContentFadeDuration))
                .Append(DOTween
                    .To(() => glowOutlineFilter.Size, x => glowOutlineFilter.Size = x, TargetSizeGlow, GlowFadeDuration).SetEase(Ease.OutQuart));
        }

        protected override IBackgroundTask OnDiscard() {
            UITweens.DiscardSequence(ref _sequence);
            return base.OnDiscard();
        }

        public UIResult Handle(UIEvent evt) {
            if (_isBought && evt is UIKeyDownAction or UIEKeyDown or UIEMouseDown) {
                Target.Discard();
                return UIResult.Accept;
            }
            
            return UIResult.Ignore;
        }
    }
}