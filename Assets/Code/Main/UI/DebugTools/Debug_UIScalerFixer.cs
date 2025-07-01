using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.DebugTools {
    public class Debug_UIScalerFixer : MonoBehaviour {
        [SerializeField] float scaleFactor = 1.5f;

        RectFixer _rectFixer;
        FontFixer _fontFixer;
        LayoutGroupFixer _layoutGroupFixer;
        LayoutElementFixer _layoutElementFixer;

        [Button("Fix All")]
        void ExecuteFixing() {
            TryFixRect();
            TryFixFontSize();
            TryFixLayoutGroup();
            TryFixLayoutElement();
        }
        
        [Button("Revert All")]
        void RevertFixing() {
            TryRevertRect();
            TryRevertFontSize();
            TryRevertLayoutGroup();
            TryRevertLayoutElement();
        }

        [Button] bool TryFixRect() => Fixer(ref _rectFixer).Execute();
        [Button] bool TryRevertRect() => Fixer(ref _rectFixer).Revert();
        
        [Button] bool TryFixFontSize() => Fixer(ref _fontFixer).Execute();
        [Button] bool TryRevertFontSize() => Fixer(ref _fontFixer).Revert();
        
        [Button] bool TryFixLayoutGroup() => Fixer(ref _layoutGroupFixer).Execute();
        [Button] bool TryRevertLayoutGroup() => Fixer(ref _layoutGroupFixer).Revert();
        
        [Button] bool TryFixLayoutElement() => Fixer(ref _layoutElementFixer).Execute();
        [Button] bool TryRevertLayoutElement() => Fixer(ref _layoutElementFixer).Revert();
        
        T Fixer<T>(ref T fixer) where T : FixCommand {
            fixer ??= Activator.CreateInstance(typeof(T), gameObject, scaleFactor) as T;
            return fixer;
        }

        interface IFixCommand {
            bool IsExecuted { get; }
            
            bool Execute();
            bool Revert();
        }
        
        abstract class FixCommand : IFixCommand {
            public bool IsExecuted { get; private set; }
            protected GameObject _target;
            protected float _scaleFactor;

            protected FixCommand(GameObject target, float scaleFactor) {
                _target = target;
                _scaleFactor = scaleFactor;
            }

            public bool Execute() {
                if (IsExecuted) return false;
                
                IsExecuted = TryFix();
                return IsExecuted;
            }

            public bool Revert() {
                if (!IsExecuted) return false;
                
                IsExecuted = !TryRevert();
                return IsExecuted;
            }
            
            protected abstract bool TryFix();
            protected abstract bool TryRevert();
        }
        
        // ReSharper disable once ClassNeverInstantiated.Local - it's instantiated via reflection
        class RectFixer : FixCommand {
            Vector2 _originalSizeDelta;
            Vector2 _originalAnchoredPos;
            
            public RectFixer(GameObject target, float scaleFactor) : base(target, scaleFactor) { }

            protected override bool TryFix() {
                if (_target.TryGetComponent(out RectTransform rectTransform)) {
                    Rect rect = rectTransform.rect;
                    _originalSizeDelta = rectTransform.sizeDelta;
                    Vector2 anchoredPos = rectTransform.anchoredPosition;
                    _originalAnchoredPos = anchoredPos;

                    if (rectTransform.anchorMin != Vector2.zero && rectTransform.anchorMax != Vector2.one) {
                        rectTransform.sizeDelta = new Vector2(rect.width * _scaleFactor, rect.height * _scaleFactor);
                        rectTransform.anchoredPosition = new Vector2(anchoredPos.x * _scaleFactor, anchoredPos.y * _scaleFactor);
                        return true;
                    }
                    
                    if (rectTransform.anchorMin.x == 0 && rectTransform.anchorMax.x == 1) {
                        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, rect.height * _scaleFactor);
                        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, anchoredPos.y * _scaleFactor);
                        return true;
                    }
                    
                    if (rectTransform.anchorMin.y == 0 && rectTransform.anchorMax.y == 1) {
                        rectTransform.sizeDelta = new Vector2(rect.width * _scaleFactor, rectTransform.sizeDelta.y);
                        rectTransform.anchoredPosition = new Vector2(anchoredPos.x * _scaleFactor, rectTransform.anchoredPosition.y);
                        return true;
                    }
                }

                return false;
            }

            protected override bool TryRevert() {
                if (_target.TryGetComponent(out RectTransform rectTransform)) {
                    rectTransform.sizeDelta = _originalSizeDelta;
                    rectTransform.anchoredPosition = _originalAnchoredPos;
                    return true;
                }

                return false;
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Local - it's instantiated via reflection
        class FontFixer : FixCommand {
            float _originalFontSize;
            float _originalMinFontSize;
            float _originalMaxFontSize;
            
            public FontFixer(GameObject target, float scaleFactor) : base(target, scaleFactor) { }

            protected override bool TryFix() {
                if (_target.TryGetComponent(out TMP_Text text)) {
                    if (text.enableAutoSizing) {
                        _originalMinFontSize = text.fontSizeMin;
                        _originalMaxFontSize = text.fontSizeMax;
                        text.fontSizeMin = Mathf.RoundToInt(text.fontSizeMin * _scaleFactor);
                        text.fontSizeMax = Mathf.RoundToInt(text.fontSizeMax * _scaleFactor);
                        return true;
                    } else {
                        _originalFontSize = text.fontSize;
                        text.fontSize = Mathf.RoundToInt(text.fontSize * _scaleFactor);
                        return true;
                    }
                }

                return false;
            }

            protected override bool TryRevert() {
                if (_target.TryGetComponent(out TMP_Text text)) {
                    if (text.enableAutoSizing) {
                        text.fontSizeMin = _originalMinFontSize;
                        text.fontSizeMax = _originalMaxFontSize;
                        return true;
                    } else {
                        text.fontSize = _originalFontSize;
                        return true;
                    }
                }

                return false;
            }
        }
        
        // ReSharper disable once ClassNeverInstantiated.Local - it's instantiated via reflection
        class LayoutGroupFixer : FixCommand {
            RectOffset _originalPadding;
            float _originalSpacing;
            
            public LayoutGroupFixer(GameObject target, float scaleFactor) : base(target, scaleFactor) { }

            protected override bool TryFix() {
                if (_target.TryGetComponent(out HorizontalOrVerticalLayoutGroup layoutGroup)) {
                    _originalPadding = layoutGroup.padding;
                    _originalSpacing = layoutGroup.spacing;
                    
                    layoutGroup.padding.left = Mathf.RoundToInt(layoutGroup.padding.left * _scaleFactor);
                    layoutGroup.padding.right = Mathf.RoundToInt(layoutGroup.padding.right * _scaleFactor);
                    layoutGroup.padding.top = Mathf.RoundToInt(layoutGroup.padding.top * _scaleFactor);
                    layoutGroup.padding.bottom = Mathf.RoundToInt(layoutGroup.padding.bottom * _scaleFactor);
                    layoutGroup.spacing = Mathf.RoundToInt(layoutGroup.spacing * _scaleFactor);
                    return true;
                }

                return false;
            }

            protected override bool TryRevert() {
                if (_target.TryGetComponent(out HorizontalOrVerticalLayoutGroup layoutGroup)) {
                    layoutGroup.padding = _originalPadding;
                    layoutGroup.spacing = _originalSpacing;
                    return true;
                }

                return false;
            }
        }
        
        // ReSharper disable once ClassNeverInstantiated.Local - it's instantiated via reflection
        class LayoutElementFixer : FixCommand {
            float _originalMinWidth;
            float _originalMinHeight;
            float _originalPreferredWidth;
            float _originalPreferredHeight;
            
            public LayoutElementFixer(GameObject target, float scaleFactor) : base(target, scaleFactor) { }

            protected override bool TryFix() {
                if (_target.TryGetComponent(out LayoutElement layoutElement)) {
                    _originalMinWidth = layoutElement.minWidth;
                    _originalMinHeight = layoutElement.minHeight;
                    _originalPreferredWidth = layoutElement.preferredWidth;
                    _originalPreferredHeight = layoutElement.preferredHeight;
                    
                    layoutElement.minWidth = Mathf.RoundToInt(layoutElement.minWidth * _scaleFactor);
                    layoutElement.minHeight = Mathf.RoundToInt(layoutElement.minHeight * _scaleFactor);
                    layoutElement.preferredWidth = Mathf.RoundToInt(layoutElement.preferredWidth * _scaleFactor);
                    layoutElement.preferredHeight = Mathf.RoundToInt(layoutElement.preferredHeight * _scaleFactor);
                    return true;
                }

                return false;
            }

            protected override bool TryRevert() {
                if (_target.TryGetComponent(out LayoutElement layoutElement)) {
                    layoutElement.minWidth = _originalMinWidth;
                    layoutElement.minHeight = _originalMinHeight;
                    layoutElement.preferredWidth = _originalPreferredWidth;
                    layoutElement.preferredHeight = _originalPreferredHeight;
                    return true;
                }

                return false;
            }
        }
    }
}
