using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Pickables {
    /// <summary>
    /// Marker element for pickable items
    /// Contains logic for clearing source of pickable item
    /// </summary>
    public partial class ItemBeingPicked : Element<Item>, ICrimeSource {
        public sealed override bool IsNotSaved => true;

        Location _locationSource;
        Pickable _pickableSource;
        
        public ItemBeingPicked(Location source) {
            _locationSource = source;
            _pickableSource = null;
        }

        public ItemBeingPicked(Pickable source) {
            _locationSource = null;
            _pickableSource = source;
        }

        public void DiscardSource() {
            _locationSource?.Discard();
            _pickableSource?.NotifyPicked();
            Discard();
        }

        ICrimeSource BackingSource => _locationSource ?? (ICrimeSource) _pickableSource;

        CrimeOwnerTemplate ICrimeSource.DefaultOwner => BackingSource.DefaultOwner;
        Faction ICrimeSource.Faction => null;

        Vector3 ICrimeSource.Position => BackingSource.Position;
        bool ICrimeSource.IsNoCrime(in CrimeArchetype archetype) => ICrimeDisabler.IsCrimeDisabled(ParentModel, archetype) || BackingSource.IsNoCrime(archetype);
        ref readonly CrimeArchetype ICrimeSource.OverrideArchetype(in CrimeArchetype archetype) => ref BackingSource.OverrideArchetype(archetype);
        float ICrimeSource.GetBountyMultiplierFor(in CrimeArchetype archetype) => BackingSource.GetBountyMultiplierFor(archetype);
    }
}