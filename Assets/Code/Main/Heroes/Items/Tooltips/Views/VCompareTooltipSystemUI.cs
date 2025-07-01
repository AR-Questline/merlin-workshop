using Awaken.TG.Main.Utility.UI.Layouts;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Views {
    public abstract class VCompareTooltipSystemUI : VBaseTooltipSystemUI, IViewCompareTooltipSystem {
        [SerializeField] CanvasGroup toCompareGroup;
        [Space(10f)] 
        [SerializeField] InterpolatedLayoutAnchor tooltipToCompareLayoutAnchor;
        
        TooltipPosition _position;
        bool _comparerVisible;
        Sequence _allAppearanceSequence;
        Sequence _toCompareAppearanceSequence;
        
        protected override void OnInitialize() {
            base.OnInitialize();
            
            toCompareGroup.alpha = 0;
            tooltipToCompareLayoutAnchor.SetWeight(0);
            tooltipToCompareLayoutAnchor.LayoutChanged += RefreshPosition;
        }

        public void ComparerAppear(bool instant) {
            _toCompareAppearanceSequence.Kill();
            if (instant) {
                toCompareGroup.alpha = 0;
                tooltipToCompareLayoutAnchor.SetWeight(1);
            } else {
                toCompareGroup.alpha = 0;
                _toCompareAppearanceSequence = DOTween.Sequence().SetUpdate(true)
                    .Append(FadeGroup(toCompareGroup, 1))
                    .Join(tooltipToCompareLayoutAnchor.TweenWeight(1, Target.AlphaTweenTime));
            }
        }

        public void ComparerDisappear(bool instant) {
            _toCompareAppearanceSequence.Kill();
            if (instant) {
                toCompareGroup.alpha = 0;
                tooltipToCompareLayoutAnchor.SetWeight(0);
            } else {
                toCompareGroup.alpha = 1;
                _toCompareAppearanceSequence = DOTween.Sequence().SetUpdate(true)
                    .Append(FadeGroup(toCompareGroup, 0))
                    .Join(tooltipToCompareLayoutAnchor.TweenWeight(0, Target.AlphaTweenTime));
            }
        }
        
        protected void SetComparerState(bool state) => toCompareGroup.gameObject.SetActive(state);
    }
}