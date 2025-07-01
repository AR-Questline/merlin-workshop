using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Thievery {
    /// <summary> Marker Element for disabling crime on this location </summary>
    public partial class NoLocationCrimeOverride : Element<Location>, ICrimeDisabler {
        public override ushort TypeForSerialization => SavedModels.NoLocationCrimeOverride;

        public bool IsNoCrime(in CrimeArchetype archetype) => true;
        Model IElement<Model>.ParentModel => ParentModel;
    }
    
    /// <summary> Marker Element for disabling crime on this item </summary>
    public partial class NoItemCrimeOverride : Element<Item>, ICrimeDisabler {
        public override ushort TypeForSerialization => SavedModels.NoItemCrimeOverride;

        bool ICrimeDisabler.IsNoCrime(in CrimeArchetype archetype) => true;
        Model IElement<Model>.ParentModel => ParentModel;
    }
}