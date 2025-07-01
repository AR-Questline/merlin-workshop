using UnityEngine;

namespace Awaken.TG.Main.Fights.Factions.Crimes {
    public interface ICrimeSource {
        Vector3 Position { get; }
        CrimeOwnerTemplate DefaultOwner { get; }
        Faction Faction { get; }
        bool IsNoCrime(in CrimeArchetype archetype);
        ref readonly CrimeArchetype OverrideArchetype(in CrimeArchetype archetype);
        float GetBountyMultiplierFor(in CrimeArchetype archetype);
    }
}