using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class AliveLocationHealthBar : Element<Location>, IWithHealthBar {
        public override ushort TypeForSerialization => SavedModels.AliveLocationHealthBar;

        public LimitedStat HealthStat => ParentModel.TryGetElement<AliveLocation>()?.Health;
    }
}