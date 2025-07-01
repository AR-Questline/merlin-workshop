using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.MVC.UI {
    public interface IFoldingViewUI : IView {
        Sequence Fold();
        Sequence Unfold();
    }
    
    public abstract class FoldingViewUI<T> : View<T>, IFoldingViewUI where T : IModel {
        protected const float ShowDuration = 0.25f;
        protected const float SequenceDelay = ShowDuration * 0.1f;

        [SerializeField] protected LayoutElement layoutElement;

        protected abstract float PreferredHeight { get; set; }
        protected virtual float DefaultHeight => 0f;

        protected Sequence _showSequence;
        protected Sequence _hideSequence;

        protected override void OnInitialize() {
            layoutElement.preferredHeight = DefaultHeight;
        }

        public virtual Sequence Fold() {
            _hideSequence.Kill();
            _showSequence.Kill();
            _showSequence = DOTween.Sequence()
                .SetUpdate(true)
                .Join(DOTween.To(() => layoutElement.preferredHeight, h => layoutElement.preferredHeight = h, PreferredHeight, ShowDuration));
            return _showSequence;
        }
        
        public virtual Sequence Unfold() {
            _showSequence.Kill();
            _hideSequence.Kill();
            _hideSequence = DOTween.Sequence()
                .SetUpdate(true)
                .Join(DOTween.To(() => layoutElement.preferredHeight, h => layoutElement.preferredHeight = h, DefaultHeight, ShowDuration));
            return _hideSequence;
        }
    }
}