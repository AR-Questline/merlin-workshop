using System;
using System.Collections.Generic;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.UI.Helpers;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.HeroCreator.ViewComponents {
    public class FoldableSettingRibbon : SelectSettingRibbon<Sprite> {
        public ARButton foldingSwitch;
        public float foldDuration;
        public Ease foldEasing;
        public RectTransform selectionBox;
        public GameObject iconPrefab;
        public ScrollRect scroll;

        public event Action<FoldableSettingRibbon> onUnfold;

        bool _unfold = false;
        List<(Image, ARButton)> _icons = new List<(Image, ARButton)>();
        RectTransform _selectedTransform;
        int _hasToUpdateSelection = -1;

        protected override void OnAttach() {
            foldingSwitch.OnClick += SwitchFolding;
            leftArrowButton.OnClick += IndexDecrement;
            rightArrowButton.OnClick += IndexIncrement;
        }

        protected override void OnChangeValue(int index, Sprite value) {
            _hasToUpdateSelection = 2;
        }

        public override void SetOptions(Sprite[] options, bool tryKeepIndex = false) {
            foreach ((Image image, ARButton button) in _icons) {
                Destroy(image.gameObject);
            }
            _icons.Clear();
            for(int i = 0; i < options.Length; i++) {
                var go = Instantiate(iconPrefab, content);
                var image = go.GetComponent<Image>();
                var button = go.GetComponent<ARButton>();
                image.sprite = options[i];
                var j = i;
                button.OnClick += () => SelectedIndex = j;
                _icons.Add((image, button));
            }

            if (options.Length == 0) {
                _unfold = false;
                TweenFolding();
            }
            gameObject.SetActive(options.Length > 0);
            base.SetOptions(options, tryKeepIndex);
        }

        public void SetUnfold(bool unfold) {
            if (unfold != _unfold) {
                SwitchFolding();
            }
        }

        void SwitchFolding() {
            if (_options.Length > 0) {
                _unfold = !_unfold;
                TweenFolding();
            }
        }

        void TweenFolding() {
            container.DOKill();
            container.DOScaleY(_unfold ? 1f : 0f, foldDuration).SetEase(foldEasing).OnUpdate(RebuildLayout);
            if (_unfold) {
                onUnfold?.Invoke(this);
            }
        }

        void RebuildLayout() {
            var rTransform = (RectTransform)transform;
            var size = rTransform.sizeDelta;
            var height = 0f;
            foreach (RectTransform child in rTransform) {
                height += child.sizeDelta.y * child.localScale.y;
            }
            size.y = height;
            rTransform.sizeDelta = size;
            LayoutRebuilder.MarkLayoutForRebuild(rTransform);
        }

        void LateUpdate() {
            if (_selectedTransform != null) {
                selectionBox.position = _selectedTransform.position;
            }

            if (_hasToUpdateSelection > 0) {
                _selectedTransform = _icons[SelectedIndex].Item1.transform as RectTransform;
                if (_selectedTransform != null) {
                    selectionBox.position = _selectedTransform.position;
                    if (_unfold) {
                        scroll.SetFocus(selectionBox);
                    }
                }

                --_hasToUpdateSelection;
            }
        }
    }
}
