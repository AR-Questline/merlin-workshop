using System;

namespace Awaken.TG.Main.UI.Popup {
    public static class PopupUIFactory { 
        public static void CreateUnsavedChangesPopup(string text, Action continueCallback, Action confirmCallback, Action exitCallback, Action backCallback) {
            PopupUI popup = null;
            popup = PopupUI.SpawnSimplePopup3Choices(typeof(VSmallPopupUI), 
                text,
                PopupUI.ConfirmTapPrompt(() => {
                    confirmCallback?.Invoke();
                    continueCallback?.Invoke();
                    popup?.Discard();
                }),
                PopupUI.ExitTapPrompt(() => {
                    exitCallback?.Invoke();
                    continueCallback?.Invoke();
                    popup?.Discard();
                }),
                PopupUI.BackTapPrompt(() => {
                    backCallback?.Invoke();
                    popup?.Discard();
                }),
                string.Empty
            );
        }
        
        public static void ConfirmPopup(string text, Action confirmCallback, Action backCallback) {
            PopupUI popup = null;
            popup = PopupUI.SpawnSimplePopup(typeof(VSmallPopupUI),
                text,
                PopupUI.ConfirmTapPrompt(() => {
                    confirmCallback?.Invoke();
                    popup?.Discard();
                }),
                PopupUI.BackTapPrompt(() => {
                    backCallback?.Invoke();
                    popup?.Discard();
                }),
                string.Empty
            );
        }
    }
}