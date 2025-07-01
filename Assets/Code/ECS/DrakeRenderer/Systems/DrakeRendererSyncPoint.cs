using Awaken.ECS.DrakeRenderer.Authoring;
using Unity.Entities;

namespace Awaken.ECS.DrakeRenderer.Systems {
    [UpdateAfter(typeof(BeginPresentationEntityCommandBufferSystem))]
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class DrakeRendererSyncPoint : SystemBase {
        protected override void OnUpdate() {
            DrakeRendererManager.Instance?.InvalidateEcb();
        }
    }
}