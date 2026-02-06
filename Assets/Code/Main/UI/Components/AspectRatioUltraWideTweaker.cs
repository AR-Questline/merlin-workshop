using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AspectRatioFitter))]
    public class AspectRatioUltraWideTweaker : MonoBehaviour {
        [SerializeField] AspectRatioFitter fitter;
        [SerializeField] float ultrawideThreshold = 1.777778f;

        void Awake() {
            float aspect = (float) Screen.width / Screen.height;
            fitter.enabled = aspect >= ultrawideThreshold;
        }

#if UNITY_EDITOR
        void Reset() {
            if (!fitter) {
                fitter = GetComponent<AspectRatioFitter>();
            }
        }
#endif
    }
}