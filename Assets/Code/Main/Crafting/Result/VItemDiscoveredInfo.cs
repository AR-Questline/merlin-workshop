using Awaken.TG.Main.Heroes.Items.Tooltips;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility.Animations;
using Awaken.Utility.Animations;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Result {
    [UsesPrefab("Crafting/Result/" + nameof(VItemDiscoveredInfo))]
    public class VItemDiscoveredInfo : View<ItemDiscoveredInfo>, IAutoFocusBase {
        const float ContentFadeTime = 0.3f;
        const float TooltipFadeTime = 0.8f;
        const float OverlapFactor = 0.4f;
        
        [field: SerializeField] public Transform ItemInfoTooltipParent { get; private set; }

        [SerializeField] TMP_Text infoText;
        [SerializeField] CanvasGroup itemInfoCanvasGroup;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        Sequence _itemDiscoveredSequence;
        Tween _tween;
        Sequence _sequence;
        
        void Awake() {
            itemInfoCanvasGroup.alpha = 0;
        }

        public void Show(string info, ItemDiscoveredTooltipSystemUI[] tooltips) { 
            infoText.text = info;

            _sequence = DOTween.Sequence().SetUpdate(true)
                .Append(itemInfoCanvasGroup.DOFade(1, ContentFadeTime));

            for (int index = 0; index < tooltips.Length; index++) {
                ItemDiscoveredTooltipSystemUI tooltip = tooltips[index];
                float insertTime = index * TooltipFadeTime * OverlapFactor;
                _sequence.Insert(insertTime, tooltip.ShowToolTip(TooltipFadeTime));
            }
        }
        
        public void Hide() {
            _sequence.Kill();
            _tween = itemInfoCanvasGroup.DOFade(0, ContentFadeTime).SetUpdate(true);
        }

        protected override IBackgroundTask OnDiscard() {
            return new TweenTask(_tween);
        }
    }
}