using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Utility.UI {
    public class VCItemsListBackground : ViewComponent {
        [SerializeField] bool disableWhenContentExceedsViewport;
        [SerializeField] RectTransform target;
        [SerializeField] RectTransform parentContent;
        [SerializeField] RectTransform elementsContent;
        [SerializeField] RectTransform viewport;
        
        float _maxHeight;
        Vector2 _targetSize;

        void Start() {
            SetupSize().Forget();
        }

        public async UniTaskVoid SetupSize() {
            if (await AsyncUtil.DelayFrame(gameObject)) {
                _targetSize = target.sizeDelta;
                _maxHeight = Mathf.Max(parentContent.sizeDelta.y, elementsContent.sizeDelta.y);
                _targetSize.y = _maxHeight;
                target.sizeDelta = _targetSize;

                if (disableWhenContentExceedsViewport) {
                    target.gameObject.SetActive(parentContent.sizeDelta.y > viewport.sizeDelta.y);
                }
            }
        }
    }
}