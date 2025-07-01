using Awaken.TG.Main.Heroes;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Factions.Crimes {
    public class FakeCrimeSource : ICrimeSource {
        readonly float _bountyMultiplier;
        readonly CrimeOwnerTemplate _owner;

        public FakeCrimeSource(CrimeOwnerTemplate owner, float bountyMultiplier) {
            _owner = owner;
            _bountyMultiplier = bountyMultiplier;
        }
            
        public Vector3 Position => Hero.Current.Coords;

        public CrimeOwnerTemplate DefaultOwner => _owner;
        public Faction Faction => null;

        public bool IsNoCrime(in CrimeArchetype archetype) => false;
        public ref readonly CrimeArchetype OverrideArchetype(in CrimeArchetype archetype) => ref archetype;
        public float GetBountyMultiplierFor(in CrimeArchetype archetype) => _bountyMultiplier;
    }
}