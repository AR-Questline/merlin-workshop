using Awaken.TG.Assets;
using Awaken.TG.Main.UIToolkit.CustomControls;
using UnityEngine;

namespace Awaken.TG.Main.Utility.UI.Keys {
    public interface IIconSearchResult { }
    
    public class SpriteIcon : IIconSearchResult {
        public SpriteReference Sprite { get; init; }
        public float AddWidthSize { get; init; }
        public float AddHeightSize { get; init; }
        public SpriteReference OverrideHoldAnimation { get; init; }
        public SpriteReference AdditionalImage { get; init; }
        public bool DisableHoldPointer { get; init; }
        public VisualOutlineFillBar.Shape HoldAnimationUTKShape { get; init; }
    }
    
    public class TextIcon : IIconSearchResult {
        public SpriteReference Background { get; init; }
        public string Text { get; init; }
        public Vector4 Margin { get; init; }
        public SpriteReference OverrideHoldAnimation { get; init; }
        public SpriteReference AdditionalImage { get; init; }
        public bool DisableHoldPointer { get; init; }
        public VisualOutlineFillBar.Shape HoldAnimationUTKShape { get; init; }
    }
}