using Awaken.TG.MVC;
using Awaken.Utility.Animations;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.Saving.Models {
    public class VSavingWorldMarker : View<SavingWorldMarker> {
        [SerializeField] GameObject logo;
        [SerializeField] CanvasGroup gameSavedCanvas;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override IBackgroundTask OnDiscard() {
            logo.SetActive(false);
            gameSavedCanvas.alpha = 1f;
            return new BackgroundUniTask(Hide());
        }
        
        public async UniTask Hide() {
            await DOTween
                .To(() => gameSavedCanvas.alpha, v => gameSavedCanvas.alpha = v, 0f, 1f)
                .SetEase(Ease.InCubic)
                .SetUpdate(true);
        }
    }
}