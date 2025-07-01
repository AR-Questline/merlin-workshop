using System;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Fights.Duels {
    public partial class HeroDuelistElement : DuelistElement, ICrimeDisabler {
        public Hero Hero => ParentModel as Hero;
        Model IElement<Model>.ParentModel => Hero;
        
        public HeroDuelistElement(DuelistsGroup group, DuelistSettings settings) : base(group, settings) { }
        
        protected override void InitDeathListener() {
            Hero.ListenTo(Hero.Events.Died, OnBeforeDeath, this);
        }
        
        protected override void OnDuelStarted() {
            if (ParentModel is Hero hero) {
                hero.Trigger(Hero.Events.ShowWeapons, true);
            }
        }

        protected override void OnVictory() {
            Hero.Trigger(Hero.Events.HideWeapons, false);
        }
        
        protected override void OnDefeat(bool forceDefeat) {
            if (forceDefeat || !Settings.fightToDeath) {
                AddElement<DefeatedHeroDuelistElement>();
            }
        }

        public bool IsNoCrime(in CrimeArchetype archetype) {
            if (!DuelController.Started) {
                return false;
            }
            
            switch (archetype.CrimeType) {
                case CrimeType.Trespassing:
                    return true;
                case CrimeType.Combat:
                    return true;
                default:
                    return false;
            }
        }
    }
}
