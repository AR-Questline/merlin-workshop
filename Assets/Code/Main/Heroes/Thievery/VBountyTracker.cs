using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Thievery {
    [UsesPrefab("HUD/Thievery/VBountyTracker")]
    public class VBountyTracker : View<BountyTracker> {
        public const float FadeTime = 0.2f;

        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] TextMeshProUGUI bountyTitleText;
        [SerializeField] TextMeshProUGUI bountyValueText;
        [SerializeField] GameObject bountyValueIcon;
        bool _fade;
        float _targetAlpha;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnHUD();

        protected override void OnInitialize() {
            bountyTitleText.text = LocTerms.Bounty.Translate();
            Target.ListenTo(BountyTracker.Events.TrackedBountyChanged, UpdateBounty, this);
            canvasGroup.alpha = 0;
        }

        void UpdateBounty(BountyTracker.BountyData bountyData) {
            if (bountyData.unforgivableCrime) {
                bountyValueText.text = LocTerms.UnforgivableCrime.Translate();
                bountyValueIcon.SetActive(false);
                Fade(1);
            } else if (bountyData.bounty > 0) {
                bountyValueText.text = bountyData.bounty.ToString("F0");
                bountyValueIcon.SetActive(true);
                Fade(1);
            } else {
                Fade(0);
            }
        }

        void Fade(float endValue) {
            _targetAlpha = endValue;
            _fade = true;
        }

        void Update() {
            if (_fade) {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, _targetAlpha, Time.deltaTime / FadeTime);
                if (math.abs(canvasGroup.alpha - _targetAlpha) < 0.0001f) {
                    _fade = false;
                }
            }
        }
    }
}