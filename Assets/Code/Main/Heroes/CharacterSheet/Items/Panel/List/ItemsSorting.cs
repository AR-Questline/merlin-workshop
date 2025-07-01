using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Tabs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List {
    public class ItemsSorting : RichEnum, IComparer<Item> {

        readonly Comparer _comparer;
        readonly bool _reverse;
        readonly LocString _name;

        public string Name => _name;

        static readonly Comparer
            ByNewest = (x, y) => Compare(x.PickupTimestamp, y.PickupTimestamp),
            ByLevel = (x, y) => Compare(x.Level, y.Level),
            ByQuality = (x, y) => Compare(x.Quality, y.Quality),
            ByQuantity = (x, y) => Compare(x.Quantity, y.Quantity),
            ByPrice = (x, y) => Compare(x.Price, y.Price),
            ByWeight = (x, y) => Compare(WeightOf(x), WeightOf(y)),
            ByDamage = (x, y) => Compare(DamageOf(x), DamageOf(y)),
            ByArmor = (x, y) => Compare(ArmorOf(x), ArmorOf(y)),
            ByBlock = (x, y) => Compare(BlockOf(x), BlockOf(y));

        [UnityEngine.Scripting.Preserve]
        public static readonly ItemsSorting
            ByNewestDescending = new(nameof(ByNewestDescending), ByNewest, false, LocTerms.ItemsComparerByNewestDescending),
            ByLevelAscending = new(nameof(ByLevelAscending), ByLevel, true, LocTerms.ItemsComparerByLevelAscending),
            ByLevelDescending = new(nameof(ByLevelDescending), ByLevel, false, LocTerms.ItemsComparerByLevelDescending),
            ByQualityAscending = new(nameof(ByQualityAscending), ByQuality, true, LocTerms.ItemsComparerByQualityAscending),
            ByQualityDescending = new(nameof(ByQualityDescending), ByQuality, false, LocTerms.ItemsComparerByQualityDescending),
            ByQuantityAscending = new(nameof(ByQuantityAscending), ByQuantity, true, LocTerms.ItemsComparerByQuantityAscending),
            ByQuantityDescending = new(nameof(ByQuantityDescending), ByQuantity, false, LocTerms.ItemsComparerByQuantityDescending),
            ByPriceAscending = new(nameof(ByPriceAscending), ByPrice, true, LocTerms.ItemsComparerByPriceAscending),
            ByPriceDescending = new(nameof(ByPriceDescending), ByPrice, false, LocTerms.ItemsComparerByPriceDescending),
            ByWeightAscending = new(nameof(ByWeightAscending), ByWeight, true, LocTerms.ItemsComparerByWeightAscending),
            ByWeightDescending = new(nameof(ByWeightDescending), ByWeight, false, LocTerms.ItemsComparerByWeightDescending),
            ByDamageDescending = new(nameof(ByDamageDescending), ByDamage, false, LocTerms.ItemsComparerByDamageDescending),
            ByArmorDescending = new(nameof(ByArmorDescending), ByArmor, false, LocTerms.ItemsComparerByArmorDescending),
            ByBlockDescending = new(nameof(ByBlockDescending), ByBlock, false, LocTerms.ItemsComparerByBlockDescending);

        ItemsSorting(string enumName, Comparer comparer, bool reverse, string nameID) : base(enumName, "ItemsComparer") {
            _comparer = comparer;
            _reverse = reverse;
            _name = new LocString { ID = nameID };
        }
        
        public int Compare(Item x, Item y) {
            int result = CompareInternal(x, y);
            return _reverse ? -result : result;
        }
        int CompareInternal(Item x, Item y) {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;
            int result = _comparer(x, y);
            return result == 0 ? Compare(x.Level, y.Level) : result;
        }

        static int Compare(long x, long y) => y.CompareTo(x);
        static int Compare(float x, float y) => y.CompareTo(x);
        static int Compare(int x, int y) => y.CompareTo(x);
        static int Compare(Stat x, Stat y) => Compare(x.ModifiedValue, y.ModifiedValue);

        static float WeightOf(Item item) => item.Weight;
        static float DamageOf(Item item) => item.Stat(ItemStatType.BaseMaxDmg);
        static float ArmorOf(Item item) => item.Stat(ItemStatType.ItemArmor);
        static float BlockOf(Item item) => item.Stat(ItemStatType.Block);

        delegate int Comparer(Item x, Item y);
        
        static readonly List<ItemsSorting> BaseComparers = new() {
            ItemsSorting.ByNewestDescending,
            ItemsSorting.ByQualityDescending,
            ItemsSorting.ByPriceDescending,
            ItemsSorting.ByWeightDescending,
        };

        
        static readonly List<ItemsSorting> WeaponComparers = BaseComparers
            .Prepend(ItemsSorting.ByDamageDescending)
            .Append(ItemsSorting.ByBlockDescending).ToList();
        static readonly List<ItemsSorting> ArmorComparers = BaseComparers.Prepend(ItemsSorting.ByArmorDescending).ToList();
        static readonly List<ItemsSorting> AllComparers = BaseComparers
            .Union(WeaponComparers)
            .Union(ArmorComparers).ToList();
        
        
        static readonly Dictionary<ItemsTabType, List<ItemsSorting>> ItemTypeSorting = new() {
            {ItemsTabType.All, AllComparers},
            {ItemsTabType.Weapons, WeaponComparers},
            {ItemsTabType.EquippableWeapons, WeaponComparers},
            {ItemsTabType.Armor, ArmorComparers},
            {ItemsTabType.QuestItems, AllComparers},
            {ItemsTabType.Others, AllComparers},
        };

        public static List<ItemsSorting> GetItemsSorting(ItemsTabType itemsTabType) {
            return ItemTypeSorting.TryGetValue(itemsTabType, out List<ItemsSorting> itemsSorting)
                ? itemsSorting
                : BaseComparers;
        }

        public static ItemsSorting DefaultSorting(ItemsTabType itemsTabType) {
            return GetItemsSorting(itemsTabType).First();
        }
    }
}