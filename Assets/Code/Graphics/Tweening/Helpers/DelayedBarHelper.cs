using System;
using Awaken.TG.Main.Utility.UI;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Graphics.Tweening.Helpers {
    [Serializable]
    public class DelayedBarHelper {
        public float delay;
        public float duration;
        public Ease easing;
        public Color increaseColor, decreaseColor;

        private float pastValue;
        private Tweener pastTweener;

        public void SetValue(float value, Image frontBar, Image backBar, bool instant = false) {
            if (instant) {
                frontBar.fillAmount = backBar.fillAmount = pastValue = value;
            } else {
                pastTweener.Kill();

                Image instantBar;
                Image delayedBar;
                if (value > pastValue) {
                    instantBar = backBar;
                    delayedBar = frontBar;
                    backBar.color = increaseColor;
                } else {
                    instantBar = frontBar;
                    delayedBar = backBar;
                    backBar.color = decreaseColor;
                }

                instantBar.fillAmount = pastValue = value;
                pastTweener = delayedBar.DOUIFill(value, duration)
                    .SetDelay(delay)
                    .SetEase(easing);
            }
        }
    }
}