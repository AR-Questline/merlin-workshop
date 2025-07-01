using Awaken.Utility;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats.Observers;
using Awaken.TG.Main.Locations.Shops;

namespace Awaken.TG.Main.Heroes.Stats {
    public sealed partial class ItemStat : Stat {
        public override ushort TypeForSerialization => SavedTypes.ItemStat;

        List<(Stat StatLevel, StatEffect StatEffect)> _allEffectors;
        bool NotAffectedByHeroStats => Owner is not Item {Owner: Hero} and not Item {Owner: Shop};
        
        public override float ModifiedValue {
            get {
                if (NotAffectedByHeroStats) {
                    return base.ModifiedValue;
                }
                return ModifiedValueWithHeroEffects();
            }
        }
        
        public ItemStat(Item owner, StatType type, float initialValue) : base(owner, type, initialValue) { }

        float ModifiedValueWithHeroEffects() {
            _allEffectors ??= Hero.Current.Element<HeroRPGStats>()
                                  .SortedStatEffectProviders()
                                  .Where(FulfillsRequirements)
                                  .ToList();

            return _allEffectors.Aggregate(
                base.ModifiedValue,
                (total, next) =>
                    next.StatEffect.EffectType?.Calculate(total, next.StatEffect.EffectStrength(next.StatLevel.ModifiedInt)) ?? total);
        }
        
        bool FulfillsRequirements((Stat StatLevel, StatEffect StatEffect) tuple) => 
            tuple.StatEffect != null && tuple.StatEffect.StatEffected == this.Type && Owner is Item i && tuple.StatEffect.ShouldApplyToItem(i);
    }
}