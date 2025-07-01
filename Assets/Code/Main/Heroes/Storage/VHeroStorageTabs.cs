using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.Heroes.Storage {
    [UsesPrefab("Storage/" + nameof(VHeroStorageTabs))]
    public class VHeroStorageTabs : View<HeroStorageTabs> { }
}
