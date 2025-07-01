using Awaken.TG.Main.UIToolkit;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.Utility.Debugging;
using DG.Tweening;
using EnhydraGames.BetterTextOutline;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.ButtonSystem {
    [UsesPrefab("UI/" + nameof(VGenericPresenterPrompt))]
    public class VGenericPresenterPrompt : View<Prompt>, IPromptListener, IUIAware {
        const float FadeDuration = 0.2f;
        const float NotActiveAlpha = 0.4f;

        [SerializeField] bool ignoreMouseClick;
        [SerializeField] VisualTreeAsset promptPrototype;

        public VisualElement Prompt { get; private set; }

        BetterOutlinedLabel _nameLabel;
        Tween _alphaTween;

        protected override bool CanNestInside(View view) => false;

        // Cause KeyIcon try to get this on attach (before on fully initialized)
        protected override void OnInitialize() {
            Prompt = promptPrototype.Instantiate();
            _nameLabel = Prompt.Q<BetterOutlinedLabel>("prompt-label");
        }

        protected override void OnMount() {
            if (GetComponentInParent<IVisualElementPromptHost>() is { } promptHost ) {
                promptHost.Add(Prompt);
            } else {
                Log.Critical?.Error($"No {nameof(IVisualElementPromptHost)} found in parent hierarchy of {gameObject.name}. Presenter prompt will not work.");
            }
        }

        public void OnTap(Prompt source) { }
        public void SetName(string name) => _nameLabel.ToUpperCase(name);
        public void SetVisible(bool visible) => Prompt.SetActiveOptimized(visible);
        
        public void SetActive(bool active) {
            if (active) {
                _alphaTween.Kill();
                _alphaTween = Prompt.DoFade(0.0f, 1.0f, FadeDuration);
            } else {
                _alphaTween.Kill();
                _alphaTween = Prompt.DoFade(0.0f, NotActiveAlpha, FadeDuration);
            }
        } 

        public UIResult Handle(UIEvent evt) {
            if (ignoreMouseClick) {
                return UIResult.Ignore;
            }

            if (evt is UIEMouseDown) {
                return UIResult.Prevent;
            }

            if (evt is UIEMouseUp { IsLeft: true }) {
                Target?.InvokeCallback();
                return UIResult.Accept;
            }

            return UIResult.Ignore;
        }
    }
}