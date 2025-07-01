using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Machines {
    /// <summary>
    /// Marker element to disable charging skill attached to Item.
    /// </summary>
    public partial class DisableSkillChargeMarker : Element<Item> {
        public override ushort TypeForSerialization => SavedModels.DisableSkillChargeMarker;
    }
}