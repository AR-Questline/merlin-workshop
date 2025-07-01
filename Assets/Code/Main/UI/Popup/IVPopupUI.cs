using Awaken.TG.Assets;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.UI.Popup.PopupContents;

namespace Awaken.TG.Main.UI.Popup {
    public interface IVPopupUI {
        void SetTitle(string title);
        void Clear();
        void SetArt(SpriteReference art);
        void ShowText(TextConfig textConfig);
        void OfferChoice(ChoiceConfig choiceConfig);
        void ToggleBg(bool enabled);
        void TogglePrompts(bool enabled);
        void SpawnContent(DynamicContent dynamicContent);
    }
}