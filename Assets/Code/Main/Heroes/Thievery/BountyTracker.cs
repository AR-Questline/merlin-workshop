using System.Linq;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Heroes.VolumeCheckers;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Heroes.Thievery {
    [SpawnsView(typeof(VBountyTracker))]
    public partial class BountyTracker : Element<Hero> {
        public sealed override bool IsNotSaved => true;

        SimpleCrimeRegion _simpleCrimeRegion;

        public new static class Events {
            public static readonly Event<BountyTracker, BountyData> TrackedBountyChanged = new(nameof(TrackedBountyChanged));
        }

        protected override void OnFullyInitialized() {
            ParentModel.ListenTo(Hero.Events.FactionRegionEntered, OnRegionChanged, this);
            ParentModel.ListenTo(Hero.Events.FactionRegionExited, OnRegionChanged, this);
            ParentModel.ListenTo(CrimeUtils.Events.CrimeCommitted, UpdateBounty, this);
            ParentModel.ListenTo(CrimeUtils.Events.UnforgivableCrimeCommittedAgainst, UpdateBounty, this);
            ParentModel.ListenTo(CrimeUtils.Events.BountyClearedFor, UpdateBounty, this);
        }

        void OnRegionChanged(RegionChangedData regionChangedData) {
            _simpleCrimeRegion = regionChangedData.CurrentRegions.FirstOrDefault();
            UpdateBounty();
        }

        void UpdateBounty() {
            float topRegionBounty = _simpleCrimeRegion ? CrimeUtils.Bounty(_simpleCrimeRegion.CrimeOwner) : 0;
            bool unforgivableCrime = _simpleCrimeRegion != null && CrimeUtils.HasCommittedUnforgivableCrime(_simpleCrimeRegion.CrimeOwner);
            this.Trigger(Events.TrackedBountyChanged, new BountyData {
                bounty = topRegionBounty,
                unforgivableCrime = unforgivableCrime
            });
        }
        
        public struct BountyData {
            public float bounty;
            public bool unforgivableCrime;
        }
    }
}