using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.General;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.Utility.Extensions;
using DG.Tweening;
using FMODUnity;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components {
    /// <summary>
    /// Button with AR UI event processing
    /// </summary>
    [AddComponentMenu("UI/ARButton", 30)]
    public class ARButton : Selectable, IUIAware {
        // === Editor fields
        [SerializeField] bool ignoreSubmitAction;
        public new Graphic targetGraphic;
        public bool allowsRMB;
        public bool ignoreRMB;
        public UIResult handleAdditionalButtons = UIResult.Prevent;
        
        // Sprite transition
        [SerializeField] FloatRange normalAlphaRange = new(0, 1);
        public Graphic hoverGraphic;
        [SerializeField] FloatRange hoverAlphaRange = new(0, 1);
        public Graphic selectedGraphic;
        [SerializeField] FloatRange selectedAlphaRange = new(0, 1);
        public Graphic additiveSelectedGraphic;
        [SerializeField] FloatRange additiveSelectedAlphaRange = new(0, 1);
        public Graphic pressGraphic;
        [SerializeField] FloatRange pressAlphaRange = new(0, 1);
        public Graphic disableGraphic;
        [SerializeField] FloatRange disableAlphaRange = new(0, 1);
        
        // Color transition
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color hoverColor = new(0.9607843f, 0.9607843f, 0.9607843f, 1);
        [SerializeField] Color selectedColor = new(0.9607843f, 0.9607843f, 0.9607843f, 1);
        [SerializeField] Color pressColor = new(0.7843137f, 0.7843137f, 0.7843137f, 1);
        [SerializeField] Color disableColor = new(0.7843137f, 0.7843137f, 0.7843137f, 0.5019608f);
        
        // Scale transition
        [SerializeField] Vector3 normalScale = new(1, 1, 1);
        [SerializeField] Vector3 hoverScale = new(1.2f, 1.2f, 1.2f);
        [SerializeField] Vector3 selectedScale = new(1.2f, 1.2f, 1.2f);
        [SerializeField] Vector3 pressScale = new(1.4f, 1.4f, 1.4f);
        [SerializeField] Vector3 disableScale = new(1, 1, 1);
        
        // Other
        [SerializeField] TransitionType transitionType = TransitionType.Color;
        public float transitionTime = 0.15f;
        // Audio Clips
        public bool disableAllSounds;
        public bool overrideSounds;
        public EventReference clickSound;
        public EventReference selectedSound;
        public bool disableInactiveSounds;
        public bool overrideInactiveSounds;
        public EventReference clickInactiveSound;

        static AudioConfig AudioConfig => CommonReferences.Get.AudioConfig;

        // === Properties
        // with backing field for refresh on change

        #region RefreshableProperties
        public Graphic TargetGraphic {
            get => targetGraphic;
            set {
                targetGraphic = value;
                MarkToRefresh();
            }
        }

        public FloatRange NormalAlphaRange {
            [UnityEngine.Scripting.Preserve] get => normalAlphaRange;
            set {
                normalAlphaRange = value;
                MarkToRefresh();
            }
        }

        public Graphic HoverGraphic {
            [UnityEngine.Scripting.Preserve] get => hoverGraphic;
            set {
                hoverGraphic = value;
                MarkToRefresh();
            }
        }
        
        public Graphic SelectedGraphic {
            [UnityEngine.Scripting.Preserve] get => selectedGraphic;
            set {
                selectedGraphic = value;
                MarkToRefresh();
            }
        }

        public FloatRange SelectedAlphaRange {
            [UnityEngine.Scripting.Preserve] get => selectedAlphaRange;
            set {
                selectedAlphaRange = value;
                MarkToRefresh();
            }
        }

        public Graphic PressGraphic {
            [UnityEngine.Scripting.Preserve] get => pressGraphic;
            set {
                pressGraphic = value;
                MarkToRefresh();
            }
        }

        public FloatRange PressAlphaRange {
            [UnityEngine.Scripting.Preserve] get => pressAlphaRange;
            set {
                pressAlphaRange = value;
                MarkToRefresh();
            }
        }

        public Graphic DisableGraphic {
            [UnityEngine.Scripting.Preserve] get => disableGraphic;
            set {
                disableGraphic = value;
                MarkToRefresh();
            }
        }

        public FloatRange DisableAlphaRange {
            [UnityEngine.Scripting.Preserve] get => disableAlphaRange;
            set {
                disableAlphaRange = value;
                MarkToRefresh();
            }
        }

        public Color NormalColor {
            get => normalColor;
            set {
                normalColor = value;
                MarkToRefresh();
            }
        }

        public Color HoverColor {
            get => hoverColor;
            set {
                hoverColor = value;
                MarkToRefresh();
            }
        }
        
        public Color SelectedColor {
            get => selectedColor;
            set {
                selectedColor = value;
                MarkToRefresh();
            }
        }

        public Color PressColor {
            get => pressColor;
            set {
                pressColor = value;
                MarkToRefresh();
            }
        }

        public Color DisableColor {
            get => disableColor;
            set {
                disableColor = value;
                MarkToRefresh();
            }
        }

        public Vector3 NormalScale {
            [UnityEngine.Scripting.Preserve] get => normalScale;
            set {
                normalScale = value;
                MarkToRefresh();
            }
        }

        public Vector3 HoverScale {
            [UnityEngine.Scripting.Preserve] get => hoverScale;
            set {
                hoverScale = value;
                MarkToRefresh();
            }
        }
        
        public Vector3 SelectedScale {
            [UnityEngine.Scripting.Preserve] get => selectedScale;
            set {
                selectedScale = value;
                MarkToRefresh();
            }
        }

        public Vector3 PressScale {
            [UnityEngine.Scripting.Preserve] get => pressScale;
            set {
                pressScale = value;
                MarkToRefresh();
            }
        }

        public Vector3 DisableScale {
            [UnityEngine.Scripting.Preserve] get => disableScale;
            set {
                disableScale = value;
                MarkToRefresh();
            }
        }

        public new TransitionType Transition {
            get => transitionType;
            set {
                transitionType = value;
                MarkToRefresh();
            }
        }
        #endregion

        // === States

        [SerializeField]new bool interactable = true;
        public bool Interactable {
            get => interactable;
            set {
                if (interactable != value) {
                    interactable = value;
                    
                    if (interactable == false) {
                        ResetState();
                    }
                    
                    OnInteractableChange?.Invoke(interactable);
                    InteractableChanged();
                }
            }
        }
        
        int _lastHoverFrame;
        bool _hovering;
        bool _internalHovering;
        public bool Hovering {
            get => _hovering;
            private set {
                if (_hovering != value) {
                    _hovering = value;
                    HoverOrSelectionChanged();
                    OnHover?.Invoke(_hovering);
                }
            }
        }

        bool _selected;
        public bool Selected {
            get => _selected;
            private set {
                if (_selected != value) {
                    _selected = value;
                    HoverOrSelectionChanged();
                    OnSelected?.Invoke(_selected);
                }
            }
        }
        
        bool _pressing;
        float _holdTime;
        readonly List<Tween> _currentTween = new();
        bool _completeTweens;
        bool _refresh;
        bool _hasGraphic;

        // === Events

        /// <summary>
        /// On start pressing button (Mouse-down at button)
        /// </summary>
        public event Action OnPress;
        /// <summary>
        /// On perform click (Mouse-down and Mouse-up at button)
        /// </summary>
        public event Action OnClick;
        /// <summary>
        /// On perform click on inactive button (Mouse-down and Mouse-up at button)
        /// </summary>
        public event Action OnClickInactive;
        /// <summary>
        /// On click canceled (Mouse-down at button and Mouse-up on other area)
        /// </summary>
        public event Action OnCancel;
        /// <summary>
        /// On button released (Mouse-up)
        /// </summary>
        public event Action OnRelease;
        /// <summary>
        /// On hover enter or exit 
        /// </summary>
        public event Action<bool> OnHover;
        /// <summary>
        /// Let the owner handle any additional event himself
        /// </summary>
        public event Func<UIEvent, UIResult> OnEvent;
        /// <summary>
        /// On selected start or end
        /// </summary>
        public event Action<bool> OnSelected;
        /// <summary>
        /// On holding call every frame with total hold time
        /// </summary>
        public event Action<float> OnHold;

        public event Action<bool> OnInteractableChange;

        protected override void Awake() {
            // This null equality check has enormous cpu cost, so for optimization we cache it
            _hasGraphic = targetGraphic != null;
            // Startup
            InteractableChanged();
            MarkTweensToComplete();
        }
        
        protected override void Start() {
            if (Application.isPlaying) {
                SetAudio();
                OnHover += isHovering => {
                    if (isHovering) {
                        RewiredHelper.VibrateUIHover(VibrationStrength.VeryLow, VibrationDuration.VeryShort);
                    }
                };
            }
        }

        void SetAudio() {
            if (disableAllSounds) {
                return;
            }

            if (!overrideSounds) {
                selectedSound = AudioConfig.ButtonSelectedSound;
                clickSound = AudioConfig.ButtonClickedSound;
            }

            if (!selectedSound.IsNull) {
                OnHover += isHovering => {
                    if (isHovering) {
                        FMODManager.PlayOneShot(selectedSound);
                    }
                };
            }

            if (!clickSound.IsNull) {
                OnClick += ClickAudioFeedback;
            }
            
            if (disableInactiveSounds) {
                return;
            }
            
            if (!overrideInactiveSounds) {
                clickInactiveSound = AudioConfig.LightNegativeFeedbackSound;
            }
            
            if(!clickInactiveSound.IsNull) {
                OnClickInactive += ClickInactiveAudioFeedback;
            }
        }
        
        void ClickAudioFeedback() {
            if (disableAllSounds || clickSound.IsNull) return;
            FMODManager.PlayOneShot(clickSound);
        }

        void ClickInactiveAudioFeedback() {
            if (disableAllSounds || disableInactiveSounds || clickInactiveSound.IsNull) return;
            FMODManager.PlayOneShot(clickInactiveSound);
        }
        
        // === Transitions

        void InteractableChanged() {
            if (!Interactable) {
                AnimateDisable();
            } else {
                AnimateNormal();
            }
        }
        
        void ResetState() {
            _pressing = false;
            _holdTime = 0;
            Hovering = false;
            Selected = false;
        }

        void HoverOrSelectionChanged() {
            if (!Interactable || _pressing) return;
            if (Selected) {
                AnimateSelected();
            } else if (_hovering) {
                AnimateHover();
            } else {
                AnimateNormal();
            }
            AnimateSelectedAdditive();
        }

        void AnimateNormal() {
            if (!Interactable) return;
            MarkTweensToComplete();
            if (_hasGraphic) {
                if (transitionType.HasFlagFast(TransitionType.Color)) {
                    AddTween(DOTween.To(() => targetGraphic.color, x => targetGraphic.color = x, normalColor, transitionTime));
                }
                if (transitionType.HasFlagFast(TransitionType.Scale)) {
                    AddTween(DOTween.To(() => transform.localScale, x => transform.localScale = x, normalScale, transitionTime));
                }
                if (transitionType.HasFlagFast(TransitionType.Sprite)) {
                    AddTweenAlpha(targetGraphic, normalAlphaRange.max);
                    AddTweenAlpha(pressGraphic, pressAlphaRange.min);
                    AddTweenAlpha(disableGraphic, disableAlphaRange.min);
                    AddTweenAlpha(hoverGraphic, hoverAlphaRange.min);
                    AddTweenAlpha(selectedGraphic, selectedAlphaRange.min);
                }
            }
        }

        void AnimateHover() {
            MarkTweensToComplete();
            if(_hasGraphic){
                if (transitionType.HasFlagFast(TransitionType.Color)) {
                    AddTween(DOTween.To(() => targetGraphic.color, x => targetGraphic.color = x, hoverColor, transitionTime));
                }
                if (transitionType.HasFlagFast(TransitionType.Scale)) {
                    AddTween(DOTween.To(() => transform.localScale, x => transform.localScale = x, hoverScale, transitionTime));
                }
                if (transitionType.HasFlagFast(TransitionType.Sprite)) {
                    AddTweenAlpha(targetGraphic, normalAlphaRange.min);
                    AddTweenAlpha(pressGraphic, pressAlphaRange.min);
                    AddTweenAlpha(disableGraphic, disableAlphaRange.min);
                    AddTweenAlpha(hoverGraphic, hoverAlphaRange.max);
                    AddTweenAlpha(selectedGraphic, selectedAlphaRange.min);
                }
            }
        }
        
        void AnimateSelected() {
            MarkTweensToComplete();
            if(_hasGraphic){
                if (transitionType.HasFlagFast(TransitionType.Color)) {
                    AddTween(DOTween.To(() => targetGraphic.color, x => targetGraphic.color = x, selectedColor, transitionTime));
                }
                if (transitionType.HasFlagFast(TransitionType.Scale)) {
                    AddTween(DOTween.To(() => transform.localScale, x => transform.localScale = x, selectedScale, transitionTime));
                }
                if (transitionType.HasFlagFast(TransitionType.Sprite)) {
                    AddTweenAlpha(targetGraphic, normalAlphaRange.min);
                    AddTweenAlpha(pressGraphic, pressAlphaRange.min);
                    AddTweenAlpha(disableGraphic, disableAlphaRange.min);
                    if (selectedGraphic != null) {
                        AddTweenAlpha(hoverGraphic, hoverAlphaRange.min);
                        AddTweenAlpha(selectedGraphic, selectedAlphaRange.max);
                    } else {
                        AddTweenAlpha(hoverGraphic, hoverAlphaRange.max);
                        AddTweenAlpha(selectedGraphic, selectedAlphaRange.min);
                    }
                }
            }
        }

        void AnimateSelectedAdditive() {
            if (_hasGraphic) {
                if (Selected) {
                    AddTweenAlpha(additiveSelectedGraphic, additiveSelectedAlphaRange.max);
                } else {
                    AddTweenAlpha(additiveSelectedGraphic, additiveSelectedAlphaRange.min);
                }
            }
        }

        void AnimatePressing() {
            if (!Interactable) return;
            MarkTweensToComplete();
            if(_hasGraphic){
                if (transitionType.HasFlagFast(TransitionType.Color)) {
                    AddTween(DOTween.To(() => targetGraphic.color, x => targetGraphic.color = x, pressColor, transitionTime));
                }
                if (transitionType.HasFlagFast(TransitionType.Scale)) {
                    AddTween(DOTween.To(() => transform.localScale, x => transform.localScale = x, pressScale, transitionTime));
                }
                if (transitionType.HasFlagFast(TransitionType.Sprite)) {
                    AddTweenAlpha(targetGraphic, normalAlphaRange.min);
                    AddTweenAlpha(pressGraphic, pressAlphaRange.max);
                    AddTweenAlpha(disableGraphic, disableAlphaRange.min);
                    AddTweenAlpha(hoverGraphic, hoverAlphaRange.min);
                    AddTweenAlpha(selectedGraphic, selectedAlphaRange.min);
                }
            }
        }

        void AnimateDisable() {
            MarkTweensToComplete();
            if (transitionType.HasFlagFast(TransitionType.Color)) {
                AddTween(DOTween.To(() => targetGraphic.color, x => targetGraphic.color = x, disableColor, transitionTime));
            }
            if (transitionType.HasFlagFast(TransitionType.Scale)) {
                AddTween(DOTween.To(() => transform.localScale, x => transform.localScale = x, disableScale, transitionTime));
            }
            if (transitionType.HasFlagFast(TransitionType.Sprite)) {
                AddTweenAlpha(targetGraphic, normalAlphaRange.min);
                AddTweenAlpha(pressGraphic, pressAlphaRange.min);
                AddTweenAlpha(disableGraphic, disableAlphaRange.max);
                AddTweenAlpha(hoverGraphic, hoverAlphaRange.min);
                AddTweenAlpha(selectedGraphic, selectedAlphaRange.min);
            }
        }

        void MarkToRefresh() {
            _refresh = true;
        }

        void Refresh() {
            if (!Interactable) {
                AnimateDisable();
            } else if (_pressing) {
                AnimatePressing();
            } else if (Selected) {
                AnimateSelected();
            } else if (Hovering) {
                AnimateHover();
            } else {
                AnimateNormal();
            }
            AnimateSelectedAdditive();
            MarkTweensToComplete();
        }

        // === Unity lifecycle

        void Update() {
            // Update hovering
            if (_lastHoverFrame < Time.frameCount - 1) {
                if (Hovering) {
                    Hovering = false;
                }
                if (_internalHovering) {
                    _internalHovering = false;
                }
            }
            
            if (!Interactable) return;
            
            // Update holding
            if (_pressing) {
                _holdTime += Time.unscaledDeltaTime;
                OnHold?.Invoke(_holdTime);
            }
        }

        void LateUpdate() {
            if (_refresh) {
                _refresh = false;
                Refresh();
            }

            if (_completeTweens) {
                _completeTweens = false;
                CompleteTweens();
            }
        }

        protected override void OnDestroy() {
            // Null all events to help GC
            CompleteTweens();
            OnPress = null;
            OnClick = null;
            OnClickInactive = null;
            OnCancel = null;
            OnRelease = null;
            OnHover = null;
            OnSelected = null;
            OnEvent = null;
            OnHold = null;
            OnInteractableChange = null;
        }

        // === Handle UI Events

        public UIResult Handle(UIEvent evt) {
            if (evt is UIEPointTo) {
                _lastHoverFrame = Time.frameCount;
                _internalHovering = true;
            }
            return Interactable ? HandleInteractiveState(evt) : HandleNonInteractiveState(evt);
        }
        
        UIResult HandleInteractiveState(UIEvent evt) {
            if (evt is UIEPointTo) {
                Hovering = true;
            }
            if (evt is UIEMouseDown md) {
                if (!CanHandle(md, out var result)) {
                    return result;
                }
                _pressing = true;
                _holdTime = 0;
                OnPress?.Invoke();
                AnimatePressing();
                return UIResult.Accept;
            }
            if (evt is UIEMouseUp mu) {
                if (!CanHandle(mu, out var result)) {
                    return result;
                }
                _pressing = false;
                OnRelease?.Invoke();
                if (Hovering) {
                    OnClick?.Invoke();
                } else {
                    OnCancel?.Invoke();
                    AnimateNormal();
                }
                return UIResult.Accept;
            }
            if (evt is UISubmitAction && !ignoreSubmitAction) {
                OnPress?.Invoke();
                OnRelease?.Invoke();
                OnClick?.Invoke();
                return UIResult.Accept;
            }

            return OnEvent?.Invoke(evt) ?? UIResult.Ignore;
        }
        
        UIResult HandleNonInteractiveState(UIEvent evt) {
            if (evt is UIEMouseDown md) {
                return !CanHandle(md, out var result) ? result : UIResult.Accept;
            }
            if (evt is UIEMouseUp mu) {
                if (!CanHandle(mu, out var result)) {
                    return result;
                }
                if (_internalHovering) {
                    OnClickInactive?.Invoke();
                } 
                return UIResult.Accept;
            }
            if (evt is UISubmitAction && !ignoreSubmitAction) {
                OnClickInactive?.Invoke();
                return UIResult.Accept;
            }
            return UIResult.Ignore;
        }
        
        bool CanHandle(UIMouseButtonEvent evt, out UIResult result) {
            if (evt.IsLeft) {
                result = UIResult.Accept;
            } else if (evt.IsRight) {
                result = ignoreRMB ? 
                    UIResult.Ignore :
                    allowsRMB ? UIResult.Accept : UIResult.Prevent;
            } else {
                result = handleAdditionalButtons;
            }

            return result == UIResult.Accept;
        }

        // === Selecting
        
        public override bool IsInteractable() => Interactable;

        protected override void DoStateTransition(SelectionState state, bool instant) {
            Selected = state == SelectionState.Selected;
        }

        // === Helpers
        public void ClearAllOnClickEvents() {
            OnClick = null;
            OnClickInactive = null;
        }

        /// <summary>
        /// Removes all audio feedback from OnClick and OnClickInactive events.  
        /// e.g. use this when handling audio feedback externally.
        /// </summary>
        public void ClearAllOnClickAudioFeedback() {
            if (OnClick != null) {
                OnClick -= ClickAudioFeedback;
            }
            
            if (OnClickInactive != null) {
                OnClickInactive -= ClickInactiveAudioFeedback;
            }
        }
        
        /// <summary>
        /// Externally invoked click audio feedback
        /// No matter if the button is interactable or not and disableAllSounds is set to true
        /// </summary>
        public void PlayClickAudioFeedback(bool isPositive, bool useLightNegativeSound = true) {
            FMODManager.PlayOneShot(isPositive ?
                AudioConfig.ButtonClickedSound : 
                useLightNegativeSound ? AudioConfig.LightNegativeFeedbackSound : AudioConfig.StrongNegativeFeedbackSound);
        }
        
        void RemoveTween(Tween tween) {
            _currentTween.Remove(tween);
        }

        void AddTween(Tween tween) {
            tween.OnComplete(() => RemoveTween(tween));
            _currentTween.Add(tween);
        }

        void AddTweenAlpha(Graphic graphic, float targetAlpha) {
            if (graphic == null) return;
            AddTween(DOTween.To(() => graphic.color.a, x => {
                Color color = graphic.color;
                color = new Color(color.r, color.g, color.b, x);
                graphic.color = color;
            }, targetAlpha, transitionTime));
        }

        void MarkTweensToComplete() {
            _completeTweens = true;
        }

        void CompleteTweens() {
            while (_currentTween.Any()) {
                Tween tween = _currentTween[0];
                tween.Complete(true);
                if (_currentTween.Any() && _currentTween[0] == tween) {
                    _currentTween.RemoveAt(0);
                }
            }
            _currentTween.Clear();
        }

        [Serializable][Flags]
        public enum TransitionType {
            Color   = (1 << 0),
            Sprite  = (1 << 1),
            Scale   = (1 << 2),
        }
    }
}
