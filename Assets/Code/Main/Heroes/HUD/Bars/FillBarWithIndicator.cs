using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.HUD.Bars {
    public class FillBarWithIndicator : SimpleBar {
        [SerializeField] Image mask;
        [SerializeField] RectTransform indicator;
        [SerializeField] bool invertIndicator;
        [SerializeField] bool vertical;

        public override void SetPercent(float percent) {
            mask.fillAmount = percent;
            
            if (invertIndicator) {
                percent = 1 - percent;
            }

            indicator.anchoredPosition = vertical
                ? new Vector2(indicator.anchoredPosition.x, bar.rectTransform.rect.height * percent)
                : new Vector2(bar.rectTransform.rect.width * percent, indicator.anchoredPosition.y);
        }
    }
}