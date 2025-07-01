using Awaken.Utility;
using System.Collections.Generic;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.Utility.Attributes;

namespace Awaken.TG.Main.Fights.Factions.Crimes {
    public partial class CrimeService : SerializedService, IDomainBoundService {
        public override ushort TypeForSerialization => SavedServices.CrimeService;
        public Domain Domain => Domain.Gameplay;
        public bool RemoveOnDomainChange() => true;
        
        [Saved] Dictionary<CrimeOwnerTemplate, CrimeOwnerData> _factionCrimes = new();
        

        public CrimeOwnerData GetCrimeData(CrimeOwnerTemplate crimeOwner) {
            if (!_factionCrimes.TryGetValue(crimeOwner, out var factionCrimes)) {
                factionCrimes = new CrimeOwnerData(crimeOwner);
                _factionCrimes[crimeOwner] = factionCrimes;
            }

            return factionCrimes;
        }
    }
}