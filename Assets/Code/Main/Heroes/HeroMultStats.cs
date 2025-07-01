using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Setup;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes {
    public partial class HeroMultStats : Element<Hero> {
        public override ushort TypeForSerialization => SavedModels.HeroMultStats;

        [Saved] HeroMultStatsWrapper _wrapper;
        
        public Stat ExpMultiplier { get; private set; }
        public Stat KillExpMultiplier { get; private set; }
        public Stat WealthMultiplier { get; private set; }
        public Stat ProfMultiplier { get; private set; }
        
        protected override void OnInitialize() {
            _wrapper.Initialize(this);
        }
        
        public static void CreateFromHeroTemplate(Hero hero) {
            HeroMultStats stats = new ();
            hero.AddElement(stats);
        }
                
        // === Persistence

        void OnBeforeWorldSerialize() {
            _wrapper.PrepareForSave(this);
        }
        
        public partial struct HeroMultStatsWrapper : INestedJsonWrapper<HeroMultStats> {
            public ushort TypeForSerialization => SavedTypes.HeroMultStatsWrapper;

            const float DefaultMultiplier = 1f;
            
            [Saved(0f)] float ExpMultiplierDif;
            [Saved(0f)] float KillExpMultiplierDif;
            [Saved(0f)] float WealthMultiplierDif;
            [Saved(0f)] float ProfMultiplierDif;

            public void Initialize(HeroMultStats heroStats) {
                Hero hero = heroStats.ParentModel;
                
                heroStats.ExpMultiplier = new Stat(hero, HeroMultStatType.ExpMultiplier, DefaultMultiplier + ExpMultiplierDif);
                heroStats.KillExpMultiplier = new Stat(hero, HeroMultStatType.KillExpMultiplier, DefaultMultiplier + KillExpMultiplierDif);
                heroStats.WealthMultiplier = new Stat(hero, HeroMultStatType.WealthMultiplier, DefaultMultiplier + WealthMultiplierDif);
                heroStats.ProfMultiplier = new Stat(hero, HeroMultStatType.ProfMultiplier, DefaultMultiplier + ProfMultiplierDif);
            }

            public void PrepareForSave(HeroMultStats heroStats) {
                ExpMultiplierDif = heroStats.ExpMultiplier.BaseValue - DefaultMultiplier;
                KillExpMultiplierDif = heroStats.KillExpMultiplier.BaseValue - DefaultMultiplier;
                WealthMultiplierDif = heroStats.WealthMultiplier.BaseValue - DefaultMultiplier;
                ProfMultiplierDif = heroStats.ProfMultiplier.BaseValue - DefaultMultiplier;
            }
        }
    }
}
