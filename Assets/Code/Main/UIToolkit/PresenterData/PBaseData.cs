using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.UI.HUD;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UIToolkit.PresenterData {
    [Serializable]
    public struct PBaseData {
        [PresenterAssetReference(new [] {typeof(VisualTreeAsset)})] 
        public ShareableARAssetReference uxml;
        [PresenterAssetReference(new [] {typeof(StyleSheet)})] [UnityEngine.Scripting.Preserve] 
        public ShareableARAssetReference[] uss;
        [UnityEngine.Scripting.Preserve] 
        public UIDocumentType documentType;
    }
}