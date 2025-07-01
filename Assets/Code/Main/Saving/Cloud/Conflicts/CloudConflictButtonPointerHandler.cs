using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Awaken.TG.Main.Saving.Cloud.Conflicts {
    public class CloudConflictButtonPointerHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler {
        [SerializeField] CloudConflictUI cloudConflictUI;
        
        public void OnPointerEnter(PointerEventData eventData) {
            if (IsButton(eventData, out Button button)) {
                cloudConflictUI.SetPointerHoveredButton(button);
            }
        }

        public void OnPointerExit(PointerEventData eventData) {
            if (IsButton(eventData, out Button _)) {
                cloudConflictUI.SetPointerHoveredButton(null);
            }
        }

        public void OnPointerDown(PointerEventData eventData) {
            if (IsButton(eventData, out Button _)) {
                cloudConflictUI.SetHoldingPointerDown(true);
            }
        }
        public void OnPointerUp(PointerEventData eventData) {
            if (IsButton(eventData, out Button _)) {
                cloudConflictUI.SetHoldingPointerDown(false);
            }
        }

        static bool IsButton(PointerEventData eventData, out Button button) => eventData.pointerEnter.TryGetComponent(out button);
    }
}