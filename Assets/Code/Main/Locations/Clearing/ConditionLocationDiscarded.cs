using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Clearing {
    [UnityEngine.Scripting.Preserve]
    public class ConditionLocationDiscarded : Condition {
        [SerializeField, DisableInPlayMode, Indent] LocationSpec location;
        
        Location Location => World.ByID<Location>(location.GetLocationId());
        
        protected override void Setup() {
            if (Location == null || Location.HasBeenDiscarded) {
                Fulfill();
            } else {
                Location.ListenTo(Model.Events.BeforeDiscarded, model => {
                    if (!model.WasDiscardedFromDomainDrop) {
                        Fulfill();
                    }
                }, Owner);
            }
        }
    }
}
