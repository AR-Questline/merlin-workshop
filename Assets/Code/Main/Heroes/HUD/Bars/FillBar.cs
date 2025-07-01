using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.HUD.Bars {
    public class FillBar : SimpleBar {
        [SerializeField] Image mask;
        
        public override void SetPercent(float percent) {
            mask.fillAmount = percent;
        }
    }
}