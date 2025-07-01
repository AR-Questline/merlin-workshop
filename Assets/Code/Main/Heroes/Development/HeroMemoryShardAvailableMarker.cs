using Awaken.TG.Main.General.NewThings;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Development {
    public partial class HeroMemoryShardAvailableMarker : Element<HeroDevelopment>, IModelNewThing {
        public override ushort TypeForSerialization => SavedModels.HeroMemoryShardAvailableMarker;

        public string NewThingId => ID;
        public bool DiscardAfterMarkedAsSeen => true;
        Hero Hero => ParentModel.ParentModel;

        protected override void OnInitialize() {
            Hero.ListenTo(Stat.Events.StatChanged(HeroStatType.WyrdMemoryShards), OnPointsChange, this);
        }

        void OnPointsChange(Stat points) {
            if (points <= 0) {
                Services.Get<NewThingsTracker>().MarkSeen(this);
            }
        }
    }
}