using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Shops.UI;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Locations.Shops.Tabs {
    public partial class ShopUITabs : Tabs<ShopUI, VShopUITabs, ShopUITabType, IShopUITab> {
        protected override KeyBindings Previous => KeyBindings.UI.Generic.Previous;
        protected override KeyBindings Next => KeyBindings.UI.Generic.Next;
    }

    public class ShopUITabType : ShopUITabs.DelegatedTabTypeEnum {
        [UnityEngine.Scripting.Preserve]
        public static readonly ShopUITabType
            Buy = new(nameof(Buy), _ => new ShopBuyUI(), Always, LocTerms.Buy),
            Sell = new(nameof(Sell), _ => new ShopSellUI(), Always, LocTerms.Sell),
            SellFromStash = new(nameof(SellFromStash), _ => new ShopSellFromStashUI(), Always, LocTerms.SellFromStash),
            Buyback = new(nameof(Buyback), _ => new ShopBuyBackUI(), Always, LocTerms.BuyBack);

        ShopUITabType(string enumName, SpawnDelegate spawn, VisibleDelegate visible, string titleID) : base(enumName, titleID, spawn, visible) { }
    }

    public interface IShopUITab : ShopUITabs.ITab { }

    public abstract partial class ShopUITab<TTabView> : ShopUITabs.Tab<TTabView>, IShopUITab where TTabView : View { }
}