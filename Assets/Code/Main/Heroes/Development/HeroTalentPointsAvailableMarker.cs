using Awaken.TG.Main.General.NewThings;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Development {
    public partial class HeroTalentPointsAvailableMarker : Element<HeroDevelopment>, IModelNewThing {
        public override ushort TypeForSerialization => SavedModels.HeroTalentPointsAvailableMarker;

        public string NewThingId => ID;
        public bool DiscardAfterMarkedAsSeen => true;
        Hero Hero => ParentModel.ParentModel;

        protected override void OnInitialize() {
            Hero.ListenTo(Stat.Events.StatChanged(CharacterStatType.TalentPoints), OnTalentPointsChange, this);
        }

        void OnTalentPointsChange(Stat talentPoints) {
            if (talentPoints <= 0) {
                Services.Get<NewThingsTracker>().MarkSeen(this);
            }
        }
    }
}