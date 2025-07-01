using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components {
    public class PushButton : Selectable {
        public bool Pressed { get; private set; }
        public float HoldTime { get; private set; }

        public Action onPress;
        public Action onCancel;
        public Action<float> onHold;

        void Update() {
            if (Pressed) {
                if (!interactable) {
                    CancelHold();
                    return;
                }

                HoldTime += Time.unscaledDeltaTime;
                onHold?.Invoke(HoldTime);
            }
        }

        public void Interrupt() {
            CancelHold();
        }

        public override void OnPointerDown(PointerEventData eventData) {
            if (!interactable) return;

            Pressed = true;
            onPress?.Invoke();
            base.OnPointerDown(eventData);
        }

        public override void OnPointerUp(PointerEventData eventData) {
            CancelHold();
            base.OnPointerUp(eventData);
        }

        void CancelHold() {
            Pressed = false;
            onCancel?.Invoke();
            HoldTime = 0f;
        }
    }
}
