using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.HUD {
    public class VCHeroHUDOverEncumbered : ViewComponent<Hero> {
        const float EncumberedGlowDuration = 0.5f;
        const float EncumberedGlowInterval = 0.1f;
        
        [SerializeField] Image encumberedIcon;
        Sequence _encumberedSequence;
        
        protected override void OnAttach() {
            Target.AfterFullyInitialized(AfterFullyInitialized);
        }
        
        void AfterFullyInitialized() {
            World.EventSystem.ListenTo(EventSelector.AnySource, HeroEncumbered.Events.EncumberedChanged, this, ToggleEncumberedIcon);
            ToggleEncumberedIcon(Target.IsEncumbered);
        }
        
        void ToggleEncumberedIcon(bool isEncumbered) {
            encumberedIcon.gameObject.SetActive(isEncumbered);
            
            if (isEncumbered) {
                ShowEncumberedGlow();
            } else {
                _encumberedSequence?.Complete();
            }
        }

        void ShowEncumberedGlow() {
            if (_encumberedSequence != null && !_encumberedSequence.IsComplete() && _encumberedSequence.IsActive()) {
                return;
            }
            
            _encumberedSequence = DOTween.Sequence();
            _encumberedSequence.Append(encumberedIcon.DOFade(1, EncumberedGlowDuration));
            _encumberedSequence.AppendInterval(EncumberedGlowInterval);
            _encumberedSequence.Append(encumberedIcon.DOFade(0, EncumberedGlowDuration));
            _encumberedSequence.SetLoops(-1, LoopType.Yoyo);
        }

        protected override void OnDiscard() {
            _encumberedSequence.Kill();
            _encumberedSequence = null;
        }
    }
}
