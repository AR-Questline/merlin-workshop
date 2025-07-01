using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Sources;
using Awaken.TG.Utility;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.TitleScreen {
    [UsesPrefab("TitleScreen/VPressStartUI")]
    public class VPressStartUI : View<PressStartUI>, IAutoFocusBase, IFocusSource, IUIAware {
        public TextMeshProUGUI text;
        Tween _animTween;
        
        public bool ForceFocus => true;
        public Component DefaultFocus => this;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            text.text = LocTerms.PopupPressAnyButton.Translate();
            World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.Keyboard, this, Target));
            _animTween = DOTween.Sequence()
                .Append(DOTween.ToAlpha(() => text.color, v => text.color = v, 0.05f, 1f).SetEase(Ease.InOutQuad))
                .Append(DOTween.ToAlpha(() => text.color, v => text.color = v, 1f, 1f).SetEase(Ease.InOutQuad))
                .SetLoops(-1);
        }

        bool IsValidEvent(UIEvent evt) => evt is UIKeyDownAction or UIEKeyDown or UIEMouseDown; 

        public UIResult Handle(UIEvent evt) {
            if (_animTween != null && IsValidEvent(evt)) {
                _animTween.Kill();
                _animTween = null;
                text.color = Color.white;
                // TODO: Audio Clip
                //AudioManager.Instance.PlayAudioClip(AudioClipName.BeorTaunt, AudioGroup.UI);
                Hide().Forget();
            }
            return UIResult.Prevent;
        }

        async UniTaskVoid Hide() {
            // var task1 = DOTween.ToAlpha(() => text.color, v => text.color = v, 0f, 1f).SetEase(Ease.InQuad).ToUniTask();
            // var task2 = DOTween.Sequence()
            //     .Append(text.transform.DOScale(1.2f, 0.1f).SetEase(Ease.OutQuad))
            //     .Append(text.transform.DOScale(1f, 0.6f).SetEase(Ease.InQuad))
            //     .ToUniTask();
            // await UniTask.WhenAll(task1, task2);
            Target.Discard();
        }
    }
}