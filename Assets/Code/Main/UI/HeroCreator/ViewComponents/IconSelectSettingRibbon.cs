using System;
using System.Collections.Generic;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.UI.Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.HeroCreator.ViewComponents {
    public class IconSelectSettingRibbon : SelectSettingRibbon<IconDescription> {
        public RectTransform selectionBox;
        public GameObject iconPrefab;
        public ScrollRect scroll;

        List<(Image, ARButton)> _icons = new List<(Image, ARButton)>();
        RectTransform _selectedTransform;

        protected override void OnAttach() {
            leftArrowButton.OnClick += IndexDecrement;
            rightArrowButton.OnClick += IndexIncrement;
        }

        protected override void OnChangeValue(int index, IconDescription value) {
            _selectedTransform = _icons[SelectedIndex].Item1.transform as RectTransform;
            if (_selectedTransform != null) {
                scroll.SetFocus(selectionBox);
            }
        }

        public override void SetOptions(IconDescription[] options, bool tryKeepIndex = false) {
            foreach ((Image image, ARButton button) in _icons) {
                Destroy(image.gameObject);
            }
            _icons.Clear();
            for(int i = 0; i < options.Length; i++) {
                var go = Instantiate(iconPrefab, content);
                var image = go.GetComponent<Image>();
                var button = go.GetComponent<ARButton>();
                options[i].TryToApply(image, go.GetComponentInChildren<TextMeshProUGUI>());
                var j = i;
                button.OnClick += () => SelectedIndex = j;
                _icons.Add((image, button));
            }
            base.SetOptions(options, tryKeepIndex);
        }

        void LateUpdate() {
            if (_selectedTransform != null) {
                selectionBox.position = _selectedTransform.position;
            }
        }
    }
}
