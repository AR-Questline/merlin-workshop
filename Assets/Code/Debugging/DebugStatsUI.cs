
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;

namespace Awaken.TG.Debugging {
    [SpawnsView(typeof(VHeroDebugStatsUI))]
    [SpawnsView(typeof(VNpcDebugStatsUI), false)]
    public partial class DebugStatsUI : Model {
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;
    }
}