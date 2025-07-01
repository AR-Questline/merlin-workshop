using System;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.Semaphores;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.Utility.Debugging;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.Components {
    public class ARMultiOptions : MonoBehaviour, IUIAware, ISemaphoreObserver {
        const float FadeDuration = 0.15f;
        
        [Title("Config")]
        [SerializeField] ARButton prevButton;
        [SerializeField] ARButton nextButton;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text valueLabel;
        
        [Title("Feedback")]
        [SerializeField] bool hoveredFeedback = true;
        [SerializeField, ShowIf(nameof(hoveredFeedback))] CanvasGroup hoverGroup;
        
        public event Action<bool> OnHovered = delegate { };
        public ARButton PrevButton => prevButton;
        public ARButton NextButton => nextButton;
        public TMP_Text NameLabel => nameLabel;
        public TMP_Text ValueLabel => valueLabel;
        public bool IsHovered { get; private set; }
        
        CoyoteSemaphore _isHovered;
        Tween _hoverTween;
        event Action PrevAction;
        event Action NextAction;

        void Awake() {
            _isHovered = new CoyoteSemaphore(this);
            hoverGroup.alpha = 0;
        }
        
        void Update() {
            _isHovered.Update();
        }

        public void Initialize(string name = null, Action prevAction = null, Action nextAction = null, string value = "") {
            SetNameText(name);
            SetValueText(value);
            
            if (prevAction != null) {
                PrevAction = prevAction;
                prevButton.OnClick += prevAction;
            }
            
            if (nextAction != null) {
                NextAction = nextAction;
                nextButton.OnClick += nextAction;
            }
        }

        public void SetNameText(string name) {
            if (string.IsNullOrEmpty(name) == false) {
                nameLabel.TrySetText(name);
            }
        }
        
        public void SetValueText(string value) {
            if (string.IsNullOrEmpty(value) == false) {
                valueLabel.TrySetText(value);
            }
        }

        void HandleHover(bool hovered) {
            if(hovered == IsHovered) return;
            IsHovered = hovered;
            
            if (hoveredFeedback) {
                _hoverTween.Kill();
                _hoverTween = hoverGroup.DOFade(hovered ? 1 : 0, FadeDuration).SetUpdate(true);
                FMODManager.PlayOneShot(World.Services.Get<CommonReferences>().AudioConfig.ButtonSelectedSound);
            }
            
            OnHovered?.Invoke(hovered);
            Log.Debug?.Info($"({gameObject.name}) Hover state {hovered}");
        }

        public UIResult Handle(UIEvent evt) {
            if (evt is UIEPointTo) {
                _isHovered.Notify();
                return UIResult.Accept;
            }
            
            if (!RewiredHelper.IsGamepad) return UIResult.Ignore;
            
            switch (evt) {
                case UIKeyDownAction action when 
                    action.Name == KeyBindings.UI.Generic.IncreaseValue:
                    NextAction?.Invoke();
                    return UIResult.Accept;
                case UIKeyDownAction action when 
                    action.Name == KeyBindings.UI.Generic.DecreaseValue:                   
                    PrevAction?.Invoke();
                    return UIResult.Accept;
                default:
                    return UIResult.Ignore;
            }
        }

        void ISemaphoreObserver.OnUp() => HandleHover(true);
        void ISemaphoreObserver.OnDown() => HandleHover(false);
        
        protected void OnDestroy() {
            KillTweens();
            OnHovered = null;
        }
        
        void KillTweens() {
            _hoverTween.Kill();
        }
    }
}
