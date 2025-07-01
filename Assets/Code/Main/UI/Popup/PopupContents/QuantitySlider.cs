using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Popup.PopupContents {
    public class QuantitySlider : Slider {
        public override void OnDrag(PointerEventData eventData) {
            if (eventData.selectedObject == gameObject) {
                base.OnDrag(eventData);
            }
        }
    }
}