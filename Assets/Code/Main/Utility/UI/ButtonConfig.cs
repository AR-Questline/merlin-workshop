using System;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components;
using Awaken.Utility;
using Awaken.Utility.GameObjects;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Utility.UI {
    public class ButtonConfig : MonoBehaviour {
        public ARButton button;
        
        [BoxGroup("Config")] [SerializeField] float animationDuration = 0.3f;
        [BoxGroup("Config")] [SerializeField, CanBeNull] TMP_Text buttonText;
        [BoxGroup("Config")] [SerializeField, CanBeNull] Image background;
        
        [Title("States")] 
        [SerializeField] bool pressed = true;
        [SerializeField] bool hovered = true;
        [SerializeField] bool interactable = true;
        [SerializeField] bool selected = true;
        
        [BoxGroup("Press")] [SerializeField, ShowIf(nameof(pressed))] bool textPressed = true;
        
        [BoxGroup("Hover")] [SerializeField, CanBeNull, ShowIf(nameof(hovered))] Image hoverDecor;
        [BoxGroup("Hover")] [SerializeField, CanBeNull, ShowIf(nameof(hovered))] CanvasGroup hoverGroup;
        [BoxGroup("Hover")] [SerializeField, CanBeNull, ShowIf(nameof(hovered))] GameObject[] hoverObjects = Array.Empty<GameObject>();
        [BoxGroup("Hover")] [SerializeField, ShowIf(nameof(hovered))] bool textHover = true;
        
        [BoxGroup("Interactable"), ShowIf(nameof(interactable))] [SerializeField] bool textInteractable = true;
        
        [BoxGroup("Select")] [SerializeField, CanBeNull, ShowIf(nameof(selected))] GameObject selectionObject;
        [BoxGroup("Select")] [SerializeField, CanBeNull, ShowIf(nameof(selected))] GameObject[] selectionObjects = Array.Empty<GameObject>();
        [BoxGroup("Select")] [SerializeField, ShowIf(nameof(selected))] bool textSelected = true;
        
        [Title("Text")]
        [SerializeField] bool translateByInspector;
        [SerializeField, ShowIf(nameof(translateByInspector)), LocStringCategory(Category.UI)] 
        LocString textString;
        [SerializeField, ShowIf(nameof(ButtonTextIsNotNull))] Color textNormalColor = ARColor.MainGrey;
        [SerializeField, ShowIf(nameof(ButtonTextIsNotNull))] Color textSelectedColor = ARColor.MainAccent;
        [SerializeField, ShowIf(nameof(ButtonTextIsNotNull))] Color textHoverColor = ARColor.MainWhite;
        [SerializeField, ShowIf(nameof(ButtonTextIsNotNull))] Color textPressedColor = ARColor.LightGrey;
        
        [Title("Image Color Transition")]
        [SerializeField, ShowIf(nameof(ButtonImageIsNotNull))] Color disabledColor = new(0.54f, 0.54f, 0.54f, 0.2f);
        [SerializeField, ShowIf(nameof(ButtonImageIsNotNull))] Color blendButtonNormalColor = new(1f, 1f, 1f, 0.314f);
        [SerializeField, ShowIf(nameof(ButtonImageIsNotNull))] Color blendButtonSelectedColor = new(1f, 1f, 1f, 1f);
        [SerializeField, ShowIf(nameof(ButtonImageIsNotNull))] Color blendButtonPressedColor = new(0.706f, 0.706f, 0.706f, 0.471f);
        
        public string Text => buttonText ? buttonText.text : string.Empty;
        public TMP_Text Label => buttonText;
        bool _isSelected;

        bool ButtonTextIsNotNull() => buttonText != null;
        bool ButtonImageIsNotNull() => background != null;
        
        public void InitializeButton(Action buttonAction = null, string buttonName = "", bool nonInteractive = false) {
            button.OnClick += buttonAction;
            SetText(string.IsNullOrEmpty(buttonName) ? textString.ToString() : buttonName);
            SetSelection(false);
            UpdateHovered(false, true);
            
            if (nonInteractive) return;

            if (pressed) button.OnPress += SetPressed;
            if (hovered) button.OnHover += state => UpdateHovered(state);
            if (selected) button.OnSelected += state => UpdateHovered(state);

            if (interactable) {
                button.OnInteractableChange += state => UpdateInteractability(state);
                UpdateInteractability(button.Interactable, true);   
            }
        }
        
        // use it in parent object and handle persistent button selection in button configs group
        public void SetSelection(bool isSelected) {
            _isSelected = isSelected;
            
            if (selectionObject) {
                selectionObject.SetActiveOptimized(isSelected);
            }
            
            if (selectionObjects is { Length: > 0 }) {
                foreach (var obj in selectionObjects) {
                    obj.SetActiveOptimized(isSelected);
                }
            }
            
            if (buttonText && textSelected) {
                buttonText.DOKill();
                buttonText.DOColor(isSelected ? textSelectedColor : textNormalColor, animationDuration).SetUpdate(true);
            }
        }

        public void SetText(string text) {
            if (buttonText && !string.IsNullOrEmpty(text)) {
                buttonText.SetText(text);
            }
        }

        void SetPressed() {
            if (background) {
                background.DOKill();
                background.DOColor(blendButtonPressedColor, animationDuration).SetUpdate(true);
            }

            if (buttonText && _isSelected == false && textPressed) {
                buttonText.DOKill();
                buttonText.DOColor(textPressedColor, animationDuration).SetUpdate(true);
            }
        }

        void UpdateHovered(bool isHovered, bool instant = false) {
            if (hoverGroup) {
                hoverGroup.DOKill();
                hoverGroup.DOFade(isHovered ? 1 : 0, animationDuration).SetUpdate(true).SetInstant(instant);
            }

            if (hoverDecor) {
                hoverDecor.DOKill();
                hoverDecor.DOFade(isHovered ? 1 : 0, animationDuration).SetUpdate(true).SetInstant(instant);
            }
            
            if (hoverObjects is { Length: > 0 }) {
                foreach (var obj in hoverObjects) {
                    obj.SetActiveOptimized(isHovered);
                }
            }

            if (background) {
                background.DOKill();
                background.DOColor(isHovered ? blendButtonSelectedColor : blendButtonNormalColor, animationDuration).SetUpdate(true).SetInstant(instant);
            }

            if (buttonText && _isSelected == false && textHover) {
                buttonText.DOKill();
                buttonText.DOColor(isHovered ? textHoverColor : textNormalColor, animationDuration).SetUpdate(true).SetInstant(instant);
            }
        }

        void UpdateInteractability(bool isInteractable, bool instant = false) {
            if (background) {
                background.DOKill();
                background.DOColor(isInteractable ? blendButtonNormalColor : disabledColor, animationDuration).SetUpdate(true).SetInstant(instant);
            }

            if (buttonText && textInteractable) {
                buttonText.DOKill();
                buttonText.DOColor(isInteractable ? textNormalColor : disabledColor, animationDuration).SetUpdate(true).SetInstant(instant);
            }
        }

        void Reset() {
            TryGetComponent(out button);
        }
    }
}