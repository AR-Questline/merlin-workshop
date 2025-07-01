using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.Cameras.CameraStack;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.GameObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Popup {
    public class VContextPopupUI : View<ContextPopupUI> {

        // === References

        public Transform actionsParent;
        public VerticalLayoutGroup verticalLayout;

        public GameObject enabledPrefab;
        public GameObject disabledPrefab;

        bool _isPositioned;
        
        // === Initialization

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            transform.position = new Vector3(9999, 9999, 9999);

            // calculate pivots to fit on the screen
            float xPivot = 0.5f;
            float yPivot = 0f;

            float xInputOnScreen = Input.mousePosition.x / Screen.width;
            float yInputOnScreen = Input.mousePosition.y / Screen.height;

            if (xInputOnScreen < 0.1f) {
                xPivot = 0f;
            } else if (xInputOnScreen > 0.9f) {
                xPivot = 1f;
            }

            if (yInputOnScreen > 0.9f) {
                yPivot = 1f;
            }

            RectTransform rectTransform = (RectTransform) verticalLayout.transform;
            rectTransform.pivot = new Vector2(xPivot, yPivot);

            // spawn options
            Target.ListenTo(Model.Events.AfterChanged, Refresh, this);
            Refresh();
        }

        // === Unity Update

        void Update() {
            if (!_isPositioned) {
                SyncPosition();
                _isPositioned = true;
            }
        }

        // === Refresh

        void Refresh() {
            GameObjects.DestroyAllChildrenSafely(actionsParent);

            foreach (ContextPopupOption option in Target.Options) {
                AddOption(option);
            }
        }

        void AddOption(ContextPopupOption option) {
            GameObject instance = Instantiate(option.enabled ? enabledPrefab : disabledPrefab, actionsParent, false);
            instance.SetActive(true);

            TextMeshProUGUI textComponent = instance.GetComponentInChildren<TextMeshProUGUI>();
            textComponent.text = option.text.ColoredText(option.color, 0.5f).FormatSprite();

            if (option.enabled) {
                ARButton button = instance.GetComponentInChildren<ARButton>();
                button.OnClick += () => Target.ChooseOption(option);
            }
        }
        
        void SyncPosition() {
            transform.position = Input.mousePosition;
        }
    }
}