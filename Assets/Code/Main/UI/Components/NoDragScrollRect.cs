using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components {
    public class NoDragScrollRect : ScrollRect {
        public override void OnBeginDrag(PointerEventData eventData) { }
        public override void OnDrag(PointerEventData eventData) { }
        public override void OnEndDrag(PointerEventData eventData) { }
    }
}