using System;
using Awaken.TG.MVC;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels {
    public class VCQuickUsePages : ViewComponent<QuickUseWheelUI> {
        const float LocalYOffset = 155f;
        
        [SerializeField] float changePageDuration = 0.2f;
        [SerializeField] Transform pagesParent;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] GameObject[] quickUsePages = Array.Empty<GameObject>();
        
        int _pageIndex;
        Sequence _nextPageSequence;
        Sequence _fadeInSequence;
        Sequence _fadeOutSequence;

        public Sequence NextPage() {
            if (quickUsePages.Length <= 1) {
                _nextPageSequence?.Kill(true);
                _nextPageSequence = DOTween.Sequence().SetUpdate(true);
                return _nextPageSequence;
            }
            
            _nextPageSequence?.Kill(true);
            _nextPageSequence = DOTween.Sequence().SetUpdate(true)
                .Append(FadeOut())
                .AppendCallback(ChangePage)
                .Append(FadeIn());

            return _nextPageSequence;
        }

        Sequence FadeIn() {
            Vector3 moveVector = LocalYOffset * Vector3.up;
            _fadeInSequence?.Kill(true);
            _fadeInSequence = DOTween.Sequence().SetUpdate(true)
                .AppendCallback(() => {
                    pagesParent.localScale = Vector3.one * 0.5f;
                    pagesParent.position -= moveVector;
                    canvasGroup.alpha = 0f;
                    GameObject page = quickUsePages[_pageIndex];
                    page.SetActive(true);
                })
                .Append(pagesParent.DOScale(Vector3.one, changePageDuration))
                .Join(pagesParent.DOLocalMove(Vector3.zero, changePageDuration))
                .Join(canvasGroup.DOFade(1f, changePageDuration));
            return _fadeInSequence;
        }

        Sequence FadeOut() {
            Vector3 moveVector = LocalYOffset * Vector3.up;
            
            _fadeOutSequence?.Kill(true);
            _fadeOutSequence = DOTween.Sequence().SetUpdate(true)
                .Append(pagesParent.DOScale(Vector3.one * 0.5f, changePageDuration))
                .Join(pagesParent.DOLocalMove(moveVector, changePageDuration))
                .Join(canvasGroup.DOFade(0f, changePageDuration))
                .AppendCallback(() => {
                    GameObject page = quickUsePages[_pageIndex];
                    page.SetActive(false);
                    pagesParent.transform.localPosition = Vector3.zero;
                });
            return _fadeOutSequence;
        }

        void ChangePage() {
            _pageIndex = (_pageIndex + 1) % quickUsePages.Length;
        }
    }
}