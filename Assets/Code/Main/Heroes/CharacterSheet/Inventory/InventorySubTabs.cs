using Awaken.TG.Main.Heroes.CharacterSheet.Items.Bag;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Loadouts;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Inventory {
    public partial class InventorySubTabs : Tabs<InventoryUI, VInventoryTabs, InventorySubTabType, IInventorySubTab> {
        protected override KeyBindings Previous => KeyBindings.UI.Generic.PreviousAlt;
        protected override KeyBindings Next => KeyBindings.UI.Generic.NextAlt;
    }
    
    public interface IInventorySubTab : InventorySubTabs.ITab { }
    public abstract partial class InventorySubTab<TTabView> : InventorySubTabs.Tab<TTabView>, IInventorySubTab where TTabView : View { }
    
    public class InventorySubTabType : InventorySubTabs.DelegatedTabTypeEnum {
        [UnityEngine.Scripting.Preserve]
        public static readonly InventorySubTabType
            Equipment = new(nameof(Equipment), LocTerms.CharacterTabEquipment, _ => new LoadoutsUI(), Always),
            Bag = new(nameof(Bag), LocTerms.CharacterTabBag, _ => new BagUI(), Always);

        protected InventorySubTabType(string enumName, string title, SpawnDelegate spawn, VisibleDelegate visible) : base(enumName, title, spawn, visible) { }
    }
}
