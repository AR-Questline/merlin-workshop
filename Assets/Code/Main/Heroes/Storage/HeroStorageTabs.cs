using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.Utility;

namespace Awaken.TG.Main.Heroes.Storage {
    public partial class HeroStorageTabs : Tabs<HeroStorageUI, VHeroStorageTabs, HeroStorageTabType, HeroStorageTabUI> {
        protected override KeyBindings Previous => KeyBindings.UI.Generic.Previous;
        protected override KeyBindings Next => KeyBindings.UI.Generic.Next;
    }
    
    public class HeroStorageTabType : HeroStorageTabs.DelegatedTabTypeEnum {
        [UnityEngine.Scripting.Preserve]
        public static readonly HeroStorageTabType
            Put = new(nameof(Put), _ => new HeroStoragePutUI(), Always, LocTerms.UIStoragePut),
            Take = new(nameof(Take), _ => new HeroStorageTakeUI(), Always, LocTerms.UIStorageTake);

        HeroStorageTabType(string enumName, SpawnDelegate spawn, VisibleDelegate visible, string titleID) : base(enumName, titleID, spawn, visible) { }
    }
}