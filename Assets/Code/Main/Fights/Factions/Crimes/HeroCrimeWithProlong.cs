using System;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Fights.Factions.Crimes {
    public partial class HeroCrimeWithProlong : DurationProxy<Hero> {
        public sealed override bool IsNotSaved => true;

        readonly Crime _crime;
        public override IModel TimeModel => ParentModel;
        
        // === Constructor
        HeroCrimeWithProlong(Crime crime, IDuration duration) : base(duration) {
            _crime = crime;
        }
        
        // === Public API
        bool CommitCrime() {
            if (_crime.TryCommitCrime()) {
                Discard();
                return true;
            }
            return false;
        }

        public virtual bool TryCommitCrime(NpcElement npc) {
            return CommitCrime();
        }

        [UnityEngine.Scripting.Preserve]
        public static void ProlongHeroCrime(Crime crime, TimeDuration duration) {
            if (!crime.IsCrime()) {
                return;
            }

            if (crime.Archetype.CrimeType == CrimeType.Murder) {
                throw new ArgumentException("Use Prolong Hero Murder instead to support corpse detection");
            }

            Hero.Current.AddElement(new HeroCrimeWithProlong(crime, duration));
        }
        
        public static void ProlongHeroMurder(Crime crime, Corpse corpse, TimeDuration duration) {
            if (!crime.IsCrime()) {
                return;
            }

            Hero.Current.AddElement(new HeroMurder(crime, corpse, duration));
        }

        public static void RemoveProlongsForFaction(CrimeOwnerTemplate crimeOwner) {
            foreach (var crimeWithProlong in Hero.Current.Elements<HeroCrimeWithProlong>().Reverse()) {
                if (crimeWithProlong._crime.Owners.Contains(crimeOwner)) {
                    crimeWithProlong.Discard();
                }
            }
        }

        public partial class HeroMurder : HeroCrimeWithProlong {
            Corpse Corpse { get; }
            
            public HeroMurder(Crime crime, Corpse corpse, IDuration duration) : base(crime, duration) {
                Corpse = corpse;
                corpse.ListenTo(Events.BeforeDiscarded, Discard, this);
            }

            public override bool TryCommitCrime(NpcElement npc) {
                return Corpse.WasViewedBy(npc.AIEntity) && base.TryCommitCrime(npc);
            }
        }
    }
}