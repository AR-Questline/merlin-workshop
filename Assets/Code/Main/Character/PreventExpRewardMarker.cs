using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Character {
    /// <summary>
    /// Marker for characters which killing should not be rewarded with experience points.
    /// </summary>
    public partial class PreventExpRewardMarker : Element<ICharacter> {
        public override ushort TypeForSerialization => SavedModels.PreventExpRewardMarker;
    }
}