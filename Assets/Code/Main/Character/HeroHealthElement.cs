using System;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Character {
    public partial class HeroHealthElement : HealthElement {
        bool _parryWindowActive;
        Action _postponedDamage;
        
        public bool HasPostponedDamage => _postponedDamage != null;
        // === Events
        public new static class Events {
            public static readonly Event<Hero, bool> HeroParryPostponeWindowStarted = new (nameof(HeroParryPostponeWindowStarted));
            public static readonly Event<Hero, bool> HeroParryPostponeWindowEnded = new (nameof(HeroParryPostponeWindowEnded));
        }

        protected override void OnInitialize() {
            if (ParentModel is not Hero hero) {
                Log.Important?.Error("HeroHealthElement attached not to Hero!!!");
                return;
            }
            
            hero.ListenTo(Events.HeroParryPostponeWindowStarted, ParryPostponeWindowStarted, this);
            hero.ListenTo(Events.HeroParryPostponeWindowEnded, ParryPostponeWindowEnded, this);
            
            base.OnInitialize();
        }
        
        protected override void TakeDamageInternal(Damage damage) {
            if (_parryWindowActive) {
                PostponeTakingDamage(damage);
                return;
            }
            base.TakeDamageInternal(damage);
        }

        void PostponeTakingDamage(Damage damage) {
            _postponedDamage += () => base.TakeDamageInternal(damage);
        }

        void ParryPostponeWindowStarted(bool _) {
            _parryWindowActive = true;
        }

        void ParryPostponeWindowEnded(bool _) {
            _parryWindowActive = false;
            _postponedDamage?.Invoke();
            _postponedDamage = null;
        }
    }
}