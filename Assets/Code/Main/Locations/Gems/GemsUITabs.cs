using System;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Gems.GemManagement;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Locations.Gems {
    public partial class GemsUITabs : Tabs<GemsUI, VGemsUITabs, GemsUITabType, IGemsUITab> {
        protected override KeyBindings Previous => KeyBindings.UI.Generic.Previous;
        protected override KeyBindings Next => KeyBindings.UI.Generic.Next;
    }
    
    public class GemsUITabType : GemsUITabs.DelegatedTabTypeEnum {
        public Type ViewType { get; }

        public static readonly GemsUITabType
            GemManagement = new(nameof(GemManagement), _ => new GemManagementUI(), gemsUI => gemsUI.CurrentType == GemManagement, LocTerms.ManageRelicsTab, typeof(VGemsUI)),
            Sharpening = new(nameof(Sharpening), _ => new SharpeningUI(), gemsUI => gemsUI.CurrentType == Sharpening, LocTerms.SharpenTab, typeof(VGearUpgradeUI)),
            Identify = new(nameof(Identify), _ => new IdentifyUI(), gemsUI => gemsUI.CurrentType == Identify, LocTerms.IdentifyTab, typeof(VGemsUI)),
            WeightReduction = new(nameof(WeightReduction), _ => new WeightReductionUI(), gemsUI => gemsUI.CurrentType == WeightReduction, LocTerms.ArmorWeightReductionTab, typeof(VGemsUI));

        GemsUITabType(string enumName, SpawnDelegate spawn, VisibleDelegate visible, string titleID, Type viewType) : base(enumName, titleID, spawn, visible) {
            ViewType = viewType;
        }
    }

    public interface IGemsUITab : GemsUITabs.ITab { }

    public abstract partial class GemsUITab<TTabView> : GemsUITabs.Tab<TTabView>, IGemsUITab where TTabView : View { }
}