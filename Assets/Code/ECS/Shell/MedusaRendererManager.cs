using System;
using Awaken.CommonInterfaces;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.LowLevel;

namespace Awaken.ECS.MedusaRenderer {
    public sealed class MedusaRendererManager : MonoBehaviourWithInitAfterLoaded,
        PlayerLoopBasedLifetime.IWithPlayerLoopEnable, PlayerLoopBasedLifetime.IWithPlayerLoopDisable,
        IMainMemorySnapshotProvider {
        public override void Init() {
            throw new NotImplementedException();
        }

        private Renderer[] _renderers;
        private int _transformsCount;
        private uint _allRenderersCount;
        private uint _allUvDistributionsCount;

        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            throw new NotImplementedException();
        }

        public readonly struct EditorAccess {
            readonly MedusaRendererManager _manager;

            public MedusaRendererManager Manager => _manager;
            public ref Renderer[] Renderers => ref _manager._renderers;
            public ref int TransformsCount => ref _manager._transformsCount;
            public ref uint AllRenderersCount => ref _manager._allRenderersCount;
            public ref uint AllUvDistributionsCount => ref _manager._allUvDistributionsCount;

            public MedusaBrgRenderer.EditorAccess BrgRenderer => throw new NotImplementedException();

            public EditorAccess(MedusaRendererManager manager) {
                throw new NotImplementedException();
            }
        }

        public void Enable() {
            throw new NotImplementedException();
        }

        public void Disable() {
            throw new NotImplementedException();
        }

        public int PreallocationSize { get; }
    }
}