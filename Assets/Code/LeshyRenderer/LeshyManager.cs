using Awaken.TG.MVC;
using Awaken.Utility.LowLevel;

namespace Awaken.TG.LeshyRenderer
{
    public sealed class LeshyManager : PlayerLoopBasedLifetimeMonoBehaviour, IService {
        protected override void OnPlayerLoopEnable() {
        }

        protected override void OnPlayerLoopDisable() {
        }

        public bool EnabledRendering { get; set; }
        public bool EnabledCells { get; set; }
        public bool EnabledCollider { get; set; }
        public bool EnabledLoading { get; set; }
        public string CatalogPath { get; set; }
        public string MatricesPath { get; set; }
    }
}