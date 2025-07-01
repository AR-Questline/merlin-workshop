using Awaken.Utility;
using System.Collections.Generic;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.Utility.Attributes;

namespace Awaken.TG.Main.Templates.Specs {
    public partial class SceneSpecCaches : SerializedService, IDomainBoundService {
        public override ushort TypeForSerialization => SavedServices.SceneSpecCaches;

        public Domain Domain => Domain.Gameplay;
        public bool RemoveOnDomainChange() => true;
        
        [Saved] HashSet<SpecId> _triggeredSpecs = new();
        
        public bool AddTriggeredSpec(SpecId specId) {
            return _triggeredSpecs.Add(specId);
        }
    }
}