using Awaken.Utility;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Observers;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Character {
    public partial class HeroRPGStats : Element<Hero> {
        public override ushort TypeForSerialization => SavedModels.HeroRPGStats;

        [Saved] HeroRPGStatsWrapper _wrapper;
        
        public Stat Strength { get; private set; }
        public Stat Dexterity { get; private set; }
        public Stat Spirituality { get; private set; }
        
        public Stat Perception { get; private set; }
        public Stat Endurance { get; private set; }
        public Stat Practicality { get; private set; }
        
        public new static class Events {
            public static readonly Event<Hero, HeroRPGStats> HeroRpgStatsFullyInitialized = new(nameof(HeroRpgStatsFullyInitialized));
        }

        protected override void OnInitialize() {
            _wrapper.Initialize(this);
            ParentModel.AfterFullyInitialized(AfterHeroFullyInitialized);
        }
        
        void AfterHeroFullyInitialized() {
            var constants = World.Services.Get<GameConstants>();
            _sortedStatEffectProviders = GetStatAndEffects(constants.rpgHeroStats)
                                         .Union(GetStatAndEffects(constants.proficiencyParams))
                                         .OrderBy(tuple => tuple.StatEffect.EffectType)
                                         .ToList();
            World.Services.Get<GameConstants>().rpgHeroStats.ForEach(p => p.AttachListeners(ParentModel, this));
            ParentModel.Trigger(Events.HeroRpgStatsFullyInitialized, this);
        }

        public static void CreateFromHero(Hero hero) {
            var heroStats = new HeroRPGStats();
            hero.AddElement(heroStats);
        }

        public List<Stat> GetHeroRPGStats() => new()
            {Strength, Endurance, Dexterity, Spirituality, Practicality, Perception};

        protected override void OnDiscard(bool fromDomainDrop) {
            _sortedStatEffectProviders = null;
        }
        
        // === Stat effects
        
        List<(Stat StatLevel, StatEffect StatEffect)> _sortedStatEffectProviders;

        public IEnumerable<(Stat StatLevel, StatEffect StatEffect)> SortedStatEffectProviders() => _sortedStatEffectProviders;

        IEnumerable<(Stat StatLevel, StatEffect StatEffect)> GetStatAndEffects<THasEffectsList>(IEnumerable<THasEffectsList> effectsList) where THasEffectsList : IStatAndEffectProvider {
            Hero hero = Hero.Current;
            return effectsList.SelectMany(effectProvider => {
                Stat stat = effectProvider.HeroStat.RetrieveFrom(hero);
                return effectProvider.Effects.Select(effect => (stat, effect));
            });
        }
        
        // === Persistence

        void OnBeforeWorldSerialize() {
            _wrapper.PrepareForSave(this);
        }
        
        public partial struct HeroRPGStatsWrapper {
            public ushort TypeForSerialization => SavedTypes.HeroRPGStatsWrapper;

            [Saved(0f)] float StrengthDif;
            [Saved(0f)] float DexterityDif;
            [Saved(0f)] float SpiritualityDif;
            [Saved(0f)] float PerceptionDif;
            [Saved(0f)] float EnduranceDif;
            [Saved(0f)] float PracticalityDif;

            public void Initialize(HeroRPGStats heroStats) {
                Hero hero = heroStats.ParentModel;
                var stats = GameConstants.Get.RPGStatParamsByType;
                
                heroStats.Strength = new Stat(hero, HeroRPGStatType.Strength, stats[HeroRPGStatType.Strength].InnateStatLevel + StrengthDif);
                heroStats.Dexterity = new Stat(hero, HeroRPGStatType.Dexterity, stats[HeroRPGStatType.Dexterity].InnateStatLevel + DexterityDif);
                heroStats.Spirituality = new Stat(hero, HeroRPGStatType.Spirituality, stats[HeroRPGStatType.Spirituality].InnateStatLevel + SpiritualityDif);
            
                heroStats.Perception = new Stat(hero, HeroRPGStatType.Perception, stats[HeroRPGStatType.Perception].InnateStatLevel + PerceptionDif);
                heroStats.Endurance = new Stat(hero, HeroRPGStatType.Endurance, stats[HeroRPGStatType.Endurance].InnateStatLevel + EnduranceDif);
                heroStats.Practicality = new Stat(hero, HeroRPGStatType.Practicality, stats[HeroRPGStatType.Practicality].InnateStatLevel + PracticalityDif);
            }

            public void PrepareForSave(HeroRPGStats heroStats) {
                var stats = GameConstants.Get.RPGStatParamsByType;
                
                StrengthDif = heroStats.Strength.ValueForSave - stats[HeroRPGStatType.Strength].InnateStatLevel;
                DexterityDif = heroStats.Dexterity.ValueForSave - stats[HeroRPGStatType.Dexterity].InnateStatLevel;
                SpiritualityDif = heroStats.Spirituality.ValueForSave - stats[HeroRPGStatType.Spirituality].InnateStatLevel;
                PerceptionDif = heroStats.Perception.ValueForSave - stats[HeroRPGStatType.Perception].InnateStatLevel;
                EnduranceDif = heroStats.Endurance.ValueForSave - stats[HeroRPGStatType.Endurance].InnateStatLevel;
                PracticalityDif = heroStats.Practicality.ValueForSave - stats[HeroRPGStatType.Practicality].InnateStatLevel;
            }
        }
    }
}