using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.MVC;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Fights.Factions.Markers {
    public partial class AntagonismDuration : DurationProxy<AntagonismMarker> {
        public override ushort TypeForSerialization => SavedModels.AntagonismDuration;

        [JsonConstructor, UnityEngine.Scripting.Preserve] AntagonismDuration() { }
        public AntagonismDuration(IDuration duration) : base(duration) { }
        public override IModel TimeModel => ParentModel.ParentModel;
        protected override void OnDiscard(bool fromDomainDrop) {
            if (ParentModel.HasBeenDiscarded) {
                return;
            }
            ParentModel.Discard();
        }
    }
}