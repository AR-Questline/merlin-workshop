using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates.Specs;
using Awaken.Utility.Maths.Data;
using Awaken.Utility.Times;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Regrowables {
    public interface IRegrowableSpec {
        GameObject gameObject { get; }
        Transform transform { get; }

        uint Count { get; }
        StoryBookmark StoryOnPickedUp { get; }

        SpecId MVCId(uint localId);
        SmallTransform Transform(uint localId);
        string RegrowablePartKey(uint localId);
        ItemSpawningData ItemReference(uint localId);
        ARTimeSpan RegrowRate(uint localId);
        CrimeOwnerTemplate CrimeOwner(uint localId);

        // Inheritance went wrong again :(
        void RegrowablePartSpawned(uint localId, GameObject spawnedRegrowablePart) {}
        void RegrowablePartDespawned(uint localId) {}
    }
}
