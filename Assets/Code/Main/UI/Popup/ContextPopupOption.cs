using System;
using UnityEngine;

namespace Awaken.TG.Main.UI.Popup {
    public class ContextPopupOption {
        public string text;
        public Color color;
        public Action callback;
        public bool enabled;
        public int sortingOrder;

        public ContextPopupOption(string text, Color color, Action callback, bool enabled = true, int sortingOrder = 0) {
            this.text = text;
            this.color = color;
            this.callback = callback;
            this.enabled = enabled;
            this.sortingOrder = sortingOrder;
        }
    }
}