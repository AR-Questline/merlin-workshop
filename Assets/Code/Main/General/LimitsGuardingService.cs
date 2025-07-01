using Awaken.Kandra;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.General {
    public class LimitsGuardingService : IService {
        const float SafeLimit = 0.75f;
        
        bool KandraIsAboveSafeLimit => BonesManagerIsAboveSafeLimit || MeshManagerVerticesAreAboveSafeLimit || MeshManagerIndicesAreAboveSafeLimit
                                       || RigManagerIsAboveSafeLimit || SkinningManagerIsAboveSafeLimit || BlendShapesManagerIsAboveSafeLimit;
        bool BonesManagerIsAboveSafeLimit => KandraRendererManager.Instance.BonesManager.FillPercentage > SafeLimit;
        bool MeshManagerVerticesAreAboveSafeLimit => KandraRendererManager.Instance.MeshManager.VerticesFillPercentage > SafeLimit;
        bool MeshManagerIndicesAreAboveSafeLimit => KandraRendererManager.Instance.MeshManager.IndicesFillPercentage > SafeLimit;
        bool RigManagerIsAboveSafeLimit => KandraRendererManager.Instance.RigManager.FillPercentage > SafeLimit;
        bool SkinningManagerIsAboveSafeLimit => KandraRendererManager.Instance.SkinningManager.FillPercentage > SafeLimit;
        bool BlendShapesManagerIsAboveSafeLimit => KandraRendererManager.Instance.BlendshapesManager.FillPercentage > SafeLimit;
        
        public void Init() {
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<NpcElement>(), this, OnNpcElementAdded);
            World.EventSystem.ListenTo(EventSelector.AnySource, GameRealTime.Events.BeforeTimeSkipped, this, BeforeTimeSkipped);
        }
        
        void OnNpcElementAdded(Model _) {
            CheckKandraLimits();
        }

        void BeforeTimeSkipped(GameRealTime.TimeSkipData _) {
            CheckKandraLimits();
        }

        void CheckKandraLimits() {
            if (!KandraIsAboveSafeLimit) {
                return;
            }
            WaitForCorrectFrameLifecycle().Forget();
        }

        async UniTaskVoid WaitForCorrectFrameLifecycle() {
            await UniTask.Yield(PlayerLoopTiming.Update);
            RemoveNpcDummies();
        }

        void RemoveNpcDummies() {
            foreach (var dummy in World.All<NpcDummy>().Reverse()) {
                dummy.TryReplaceWithSimplifiedLocation();
            }
        }
    }
}
