using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Fights.Factions.Crimes {
    public partial class TemporaryBounty : Element<IllegalActionTracker> {
        public const int CrimeApplyDelay = 45;
        readonly MultiMap<NpcElement, CrimeNpcContainer> _crimes = new();
        readonly List<CrimeNpcContainer> _crime = new(3);

        public sealed override bool IsNotSaved => true;
        
        float _timeToApplyCrime;

        [CanBeNull]
        public static TemporaryBounty TryGet() => Hero.Current.Element<IllegalActionTracker>().TryGetElement<TemporaryBounty>();
        [NotNull]
        public static TemporaryBounty GetOrCreate() => Hero.Current.Element<IllegalActionTracker>().AddMarkerElement<TemporaryBounty>();

        protected override void OnFullyInitialized() {
            base.OnFullyInitialized();
            this.GetOrCreateTimeDependent().WithUpdate(OnUpdate);
            _timeToApplyCrime = CrimeApplyDelay;
            
            var saveBlocker = new SaveBlocker("TemporaryBounty");
            World.Add(saveBlocker);
            this.ListenTo(Events.BeforeDiscarded, saveBlocker.Discard, this);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            this.GetTimeDependent()?.WithoutUpdate(OnUpdate);
            base.OnDiscard(fromDomainDrop);
        }

        public void RegisterCrime(List<NpcElement> owners, in Crime crime) {
            var crimeContainer = new CrimeNpcContainer(owners, crime);
            foreach (NpcElement owner in owners) {
                if (!_crimes.ContainsKey(owner)) {
                    owner.ListenTo(Events.AfterDiscarded, OnOwnerDiscarded, this);
                }
                _crimes.Add(owner, crimeContainer);
            }
            _crime.Add(crimeContainer);
            Log.Debug?.Info($"[Thievery] Temp Bounty: {crime.Bounty(owners[0].GetCurrentCrimeOwnersFor(crime.Archetype).PrimaryOwner)} from crime: {crime.Archetype} for {owners.Count} owners");
        }

        void OnOwnerDiscarded(Model obj) {
            if (obj.WasDiscardedFromDomainDrop) return;
            
            var owner = (NpcElement) obj;
            if (!_crimes.Remove(owner, out HashSet<CrimeNpcContainer> containers)) {
                Log.Important?.Error($"[Thievery] Temp Bounty: Owner {owner} was discarded but not found in crimes");
                return;
            }

            foreach (CrimeNpcContainer container in containers) {
                container.Owners.Remove(owner);
                if (container.Owners.Count == 0) {
                    _crime.Remove(container);
                    if (_crime.Count == 0) {
                        Discard();
                        Log.Debug?.Info("[Thievery] Temp Bounty: All owners discarded");
                    }
                }
            }
        }

        void OnUpdate(float deltatime) {
            if (_timeToApplyCrime > 0) {
                _timeToApplyCrime -= deltatime;
                if (_timeToApplyCrime <= 0) {
                    ApplyCrimes();
                }
            }
        }

        public void GuardApplyCrimes() {
            ApplyCrimes();
        }

        void ApplyCrimes() {
            foreach (Crime crime in _crime.Select(c => c.Crime)) {
                crime.TryCommitCrime(CrimeSituation.InstantReport | CrimeSituation.IgnoresVisibility | CrimeSituation.SkipsWatchingNPCs);
            }
            _crimes.Clear();
            _crime.Clear();
            Discard();
        }
        
        class CrimeNpcContainer {
            public List<NpcElement> Owners { get; }
            public Crime Crime { get; }

            public CrimeNpcContainer(List<NpcElement> owners, in Crime crime) {
                Owners = owners;
                Crime = crime;
            }
        }
    }
}