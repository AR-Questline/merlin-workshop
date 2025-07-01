using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.Components {
    public class TMP_Paginated : TextMeshProUGUI {
        [SerializeField] ARButton pageLeft;
        [SerializeField] ARButton pageRight;

        public void PageRight() {
            pageToDisplay++;
            UpdatePageButtons();
        }

        public void PageLeft() {
            pageToDisplay--;
            UpdatePageButtons();
        }

        protected override void Awake() {
            AsyncAwake().Forget();
        }

        protected async UniTaskVoid AsyncAwake() {
            base.Awake();
            pageToDisplay = 1;
            await UniTask.DelayFrame(1);

            if (m_textInfo.pageCount == 1) return;
            
            UpdatePageButtons();

            pageLeft.OnClick += PageLeft;
            pageRight.OnClick += PageRight;
        }

        void UpdatePageButtons() {
            pageRight.Interactable = m_textInfo.pageCount != m_pageToDisplay;
            pageLeft.Interactable = m_pageToDisplay != 1;
        }
    }
}