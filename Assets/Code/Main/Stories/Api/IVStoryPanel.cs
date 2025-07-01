using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.UI.Popup.PopupContents;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Api {
    /// <summary>
    ///     Interface for views spawned for Story model.
    ///     Each Story can have a different View and access it using this interface.
    /// </summary>
    public interface IVStoryPanel {
        void Clear();
        void SetArt(SpriteReference art);
        void SetTitle(string title);
        void ClearText();
        void ShowText(TextConfig textConfig);
        void ShowLastChoice(string textToDisplay, string iconName);
        void ShowChange(Stat stat, int change);
        void OfferChoice(ChoiceConfig choiceConfig);
        void ToggleBg(bool bgEnabled);
        void ToggleViewBackground(bool enabled);
        void TogglePrompts(bool promptsEnabled);
        Transform LastChoicesGroup();
        Transform StatsPreviewGroup();
        void SpawnContent(DynamicContent dynamicContent);
        void LockChoiceAssetGate();
        void UnlockChoiceAssetGate();
    }
}