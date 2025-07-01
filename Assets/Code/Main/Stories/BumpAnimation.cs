using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.Stories {
    public class BumpAnimation : MonoBehaviour {
        public float endScaleValue = 1.2f;
        public float duration = 0.6f;
        void Start() {
            transform.DOScale(endScaleValue, duration).SetLoops(-1, LoopType.Yoyo);
        }
    }
}
