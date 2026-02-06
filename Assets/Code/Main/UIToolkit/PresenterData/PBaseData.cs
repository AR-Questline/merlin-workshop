using System;
using Awaken.TG.Assets;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UIToolkit.PresenterData {
    [Serializable]
    public struct PBaseData {
        [PresenterAssetReference(new [] {typeof(VisualTreeAsset)})] 
        public ShareableARAssetReference uxml;
        [UnityEngine.Scripting.Preserve] 
        public UIDocumentType documentType;
    }
}