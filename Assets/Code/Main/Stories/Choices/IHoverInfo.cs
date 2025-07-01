using Awaken.TG.Assets;

namespace Awaken.TG.Main.Stories.Choices {
    public interface IHoverInfo {
        string InfoGroupName { get; set; }
        string InfoName { get; set; }
        string InfoDescription{ get; set; }
        ShareableSpriteReference InfoIcon { get; set; }
    }
}