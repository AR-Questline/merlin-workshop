using Awaken.TG.Main.Utility.Availability;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Pickables {
    [ExecuteAlways, SelectionBase]
    public class PickableSpec : PickableSpecBase {
        [SerializeField] DayNightAvailability availability;
        
        protected override AvailabilityBase Availability => availability;
    }
}