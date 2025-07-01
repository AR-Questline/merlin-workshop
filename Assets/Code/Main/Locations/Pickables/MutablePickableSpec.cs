using Awaken.TG.Main.Utility.Availability;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Pickables {
    [ExecuteAlways, SelectionBase]
    public class MutablePickableSpec : PickableSpecBase {
        [SerializeField] MutableAvailability availability;
        
        protected override AvailabilityBase Availability => availability;
    }
}