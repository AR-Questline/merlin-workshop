using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations.Shops {
    public partial class MerchantStats : Element<IMerchant> {
        public override ushort TypeForSerialization => SavedModels.MerchantStats;

        [Saved] MerchantStatsWrapper _wrapper;
        
        // === Trades
        
        /// <summary>
        /// It modifies the price when this merchant buys item
        /// </summary>
        public Stat BuyModifier { get; private set; }
        
        /// <summary>
        /// It modifies the price when this merchant sells item
        /// </summary>
        public Stat SellModifier { get; private set; }

        // === Currencies
        
        public CurrencyStat Wealth { get; private set; }
        public CurrencyStat Cobweb {get; private set; }

        // === Creation
        
        protected override void OnInitialize() {
            _wrapper.Initialize(this);
        }

        public static void Create(IMerchant merchant) {
            var economyStats = new MerchantStats();
            merchant.AddElement(economyStats);
        }

        // === Persistence

        void OnBeforeWorldSerialize() {
            _wrapper.PrepareForSave(this);
        }
        
        public partial struct MerchantStatsWrapper {
            public ushort TypeForSerialization => SavedTypes.MerchantStatsWrapper;

            [Saved(0f)] float BuyModifierDif;
            [Saved(0f)] float SellModifierDif;
            [Saved(0f)] float WealthDif;
            [Saved(0f)] float CobwebDif;

            public void Initialize(MerchantStats stats) {
                IMerchant owner = stats.ParentModel;
                GetInitialValues(stats, out float buyModifier, out float sellModifier, out float wealth, out float cobweb);

                stats.BuyModifier = new Stat(owner, TradeStatType.BuyModifier, buyModifier + BuyModifierDif);
                stats.SellModifier = new Stat(owner, TradeStatType.SellModifier, sellModifier + SellModifierDif);
                stats.Wealth = new CurrencyStat(owner, CurrencyStatType.Wealth, wealth + WealthDif);
                stats.Cobweb = new CurrencyStat(owner, CurrencyStatType.Cobweb, cobweb + CobwebDif);
            }

            public void PrepareForSave(MerchantStats merchantStats) {
                GetInitialValues(merchantStats, out float buyModifier, out float sellModifier, out float wealth, out float cobweb);
                
                BuyModifierDif = merchantStats.BuyModifier.ValueForSave - buyModifier;
                SellModifierDif = merchantStats.SellModifier.ValueForSave - sellModifier;
                WealthDif = merchantStats.Wealth.ValueForSave - wealth;
                CobwebDif = merchantStats.Cobweb.ValueForSave - cobweb;
            }
            
            void GetInitialValues(MerchantStats merchantStats, out float buyModifier, out float sellModifier, out float wealth, out float cobweb) {
                buyModifier = 1f;
                sellModifier = 1f;
                wealth = 1f;
                cobweb = 0f;
                
                if (merchantStats.ParentModel is Shop shop) {
                    buyModifier = shop.Template.buyModifier;
                    sellModifier = shop.Template.sellModifier;
                }
                else if (merchantStats.ParentModel is Hero hero) {
                    buyModifier = hero.Template.buyModifier;
                    sellModifier = hero.Template.sellModifier;
                }
            }
        }
    }
    
    public enum CurrencyType : byte {
        Money,
        Cobweb
    }
}