using System;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Utility.Semaphores;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.Utility.Debugging;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.Components {
    public class ARInputField : MonoBehaviour, IUIAware, ISemaphoreObserver {
        const float FadeDuration = 0.15f;
        
        [Title("Config")]
        [SerializeField] TMP_InputField inputField;
        
        [Title("Feedback")]
        [SerializeField] bool hoveredFeedback = true;
        [SerializeField, ShowIf(nameof(hoveredFeedback))] CanvasGroup hoverGroup;
        [SerializeField] bool selectedFeedback = true;
        [SerializeField,ShowIf(nameof(selectedFeedback))] CanvasGroup selectionGroup;
        
        public TMP_InputField TMPInputField => inputField;
        public event Action<bool> OnSelected = delegate { };
        public event Action<bool> OnHovered = delegate { };
        public bool IsSelected { get; private set; }
        public bool IsHovered { [UnityEngine.Scripting.Preserve] get; private set; }
        
        CoyoteSemaphore _isHovered;
        Tween _hoverTween;

        void Awake() {
            _isHovered = new CoyoteSemaphore(this);
            hoverGroup.alpha = 0;        
        }
        
        void Update() {
            _isHovered.Update();
        }

        public void Initialize(string placeholder = null, string value = null, Action<string> onValueChanged = null) {
            if (string.IsNullOrEmpty(placeholder) == false && inputField.placeholder.TryGetComponent<TMP_Text>(out var placeholderLabel)) {
                placeholderLabel.text = placeholder;
            }
            
            if (string.IsNullOrEmpty(value) == false) {
                inputField.text = value;
            }
            
            if (onValueChanged != null) {
                inputField.onValueChanged.AddListener(onValueChanged.Invoke);
            }
            
            inputField.onSubmit.AddListener(_ => HandleSelection(false));
            inputField.onSelect.AddListener(_ => HandleSelection(true));
            inputField.onDeselect.AddListener(_ => HandleSelection(false));
        }

        public void InvokeSelect(bool selected, bool invokeEvent = true) => HandleSelection(selected, invokeEvent);
        
        void HandleSelection(bool selected, bool invokeCallback = true) {
            IsSelected = selected;
            
            if (selectedFeedback) {
                selectionGroup.alpha = selected ? 1 : 0;
            }
            
            if (invokeCallback) {
                OnSelected?.Invoke(selected);
            }
            
            Log.Debug?.Info($"({gameObject.name}) Selected state: {selected}. Invoke callback state: {invokeCallback}");
        }

        void HandleHover(bool hovered) {
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
            
            return UIResult.Ignore;
        }

        void ISemaphoreObserver.OnUp() => HandleHover(true);
        void ISemaphoreObserver.OnDown() => HandleHover(false);
        
        protected void OnDestroy() {
            KillTweens();
            OnSelected = null;
            OnHovered = null;
        }
        
        void KillTweens() {
            _hoverTween.Kill();
        }
    }
}
