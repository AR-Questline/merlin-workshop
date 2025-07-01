using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.ButtonSystem {
    [UsesPrefab("UI/VGenericPromptUI")]
    public class VGenericPromptUI : View<Prompt>, IPromptListener, IUIAware {
        const float FadeDuration = 0.2f;
        const float NotActiveAlpha = 0.4f;
        
        [SerializeField] CanvasGroup group;
        [SerializeField] TextMeshProUGUI action;
        [SerializeField] bool ignoreMouseClick;

        Tween _alphaTween;
        
        protected override bool CanNestInside(View view) => false;

        public void OnTap(Prompt source) { }

        public void SetName(string name) {
            if (action != null) {
                action.text = name;
            }
        }

        public void SetActive(bool active) {
            _alphaTween.Kill();
            _alphaTween = active
                ? group.DOFade(1f, FadeDuration).SetUpdate(true)
                : group.DOFade(NotActiveAlpha, FadeDuration).SetUpdate(true);
        }

        public void SetVisible(bool visible) {
            gameObject.SetActive(visible);
        }

        public UIResult Handle(UIEvent evt) {
            if (ignoreMouseClick) {
                return UIResult.Ignore;
            }

            if (Target is {IsActive: true} && evt is UIMouseButtonEvent {IsLeft: true} mouseEvent) {
                return Target.ParentModel.HandleMouse(Target, mouseEvent);
            }

            return UIResult.Ignore;
        }
    }
}