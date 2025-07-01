using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.UI.HeroRendering {
    [SpawnsView(typeof(VWorldHeroRenderer))]
    public partial class WorldHeroRenderer : HeroRendererBase {
        public override uint? LightRenderLayerMask => null;
        public override int? WeaponLayer => null;
        
        public WorldHeroRenderer(bool useLoadoutAnimations) : base(useLoadoutAnimations) { }
    }
}