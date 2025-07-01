using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.MVC;
using Awaken.Utility;

namespace Awaken.TG.Main.Fights.Factions {
    public partial class ParanoiaEvent : DurationProxy<Hero> {
        public override ushort TypeForSerialization => SavedModels.ParanoiaEvent;

        public override IModel TimeModel => ParentModel;
    }
}