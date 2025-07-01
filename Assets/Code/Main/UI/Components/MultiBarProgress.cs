using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components {
    /// <summary>
    /// Used for stats UI components that can show predicted change on the image.
    /// </summary>
    public class MultiBarProgress : MonoBehaviour {

        // === Fields

        public Image backBar;
        public Image frontBar;

        public float minValue;
        public float maxValue;

        // === Public methods

        public void Set(float value) {
            value = Mathf.Lerp(minValue, maxValue, value);
            backBar.fillAmount = value;
            frontBar.fillAmount = value;
        }

        public void Set(float value, float predictedChange) {
            value = Mathf.Lerp(minValue, maxValue, value);
            predictedChange = predictedChange * (maxValue - minValue);

            if (predictedChange > 0f) {
                backBar.fillAmount = value + predictedChange;
                frontBar.fillAmount = value;
            } else {
                backBar.fillAmount = value;
                frontBar.fillAmount = value + predictedChange;
            }
        }
    }
}