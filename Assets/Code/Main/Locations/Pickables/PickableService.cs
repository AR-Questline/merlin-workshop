using Awaken.Utility;
using System.Collections.Generic;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Templates.Specs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.Utility.Attributes;

namespace Awaken.TG.Main.Locations.Pickables {
    public partial class PickableService : SerializedService, IDomainBoundService {
        public override ushort TypeForSerialization => SavedServices.PickableService;
        public Domain Domain => Domain.CurrentMainScene();
        public bool RemoveOnDomainChange() => true;
        
        [Saved] HashSet<SpecId> _pickedPickables = new();

        public bool WasPicked(Pickable pickable) {
            return _pickedPickables.Contains(pickable.Id);
        }
        
        public void NotifyPicked(Pickable pickable) {
            _pickedPickables.Add(pickable.Id);
        }
    }
}