using System;
using Awaken.CommonInterfaces;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.LowLevel;
using UnityEngine;

namespace Awaken.ECS.MedusaRenderer {
    public sealed class MedusaRendererManager : MonoBehaviourWithInitAfterLoaded,
        PlayerLoopBasedLifetime.IWithPlayerLoopEnable, PlayerLoopBasedLifetime.IWithPlayerLoopDisable, IMainMemorySnapshotProvider {
        [SerializeField] Renderer[] _renderers;
        [SerializeField] int _transformsCount;
        [SerializeField] uint _allRenderersCount;
        [SerializeField] uint _allUvDistributionsCount;

        bool _initialized;
        MedusaBrgRenderer _medusaBrgRenderer;

        // === Initialization
        public override void Init() {
            _initialized = true;
            PlayerLoopBasedLifetime.Instance.ScheduleEnable(this);
        }

        void OnEnable() {
            if (_initialized) {
                PlayerLoopBasedLifetime.Instance.ScheduleEnable(this);
            }
        }

        void OnDisable() {
            PlayerLoopBasedLifetime.Instance.ScheduleDisable(this);
        }

        void PlayerLoopBasedLifetime.IWithPlayerLoopEnable.Enable() {
            if (_transformsCount == 0 || _medusaBrgRenderer != null) {
                return;
            }
            _medusaBrgRenderer = new MedusaBrgRenderer(_transformsCount, gameObject.scene.name);
            _medusaBrgRenderer.SetTransforms(_transformsCount);
            _medusaBrgRenderer.SetRenderers(_renderers, _allRenderersCount, _allUvDistributionsCount);
            IMainMemorySnapshotProvider.RegisterProvider(this);
        }

        void PlayerLoopBasedLifetime.IWithPlayerLoopDisable.Disable() {
            _medusaBrgRenderer?.Dispose();
            _medusaBrgRenderer = null;
            IMainMemorySnapshotProvider.UnregisterProvider(this);
        }

        // === IMainMemorySnapshotProvider
        public int PreallocationSize => 1_000;

        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            if (!_initialized) {
                ownPlace.Span[0] = new MemorySnapshot("MedusaRendererManager", 0, 0);
                return 0;
            }
            var childrenCount = 1;

            var renderersSize = 0L;
            foreach (var rendererData in _renderers) {
                // renderData field
                var rendererDatumSize = 2 * IntPtr.Size + sizeof(ushort); // Two pointers and submesh
                renderersSize += rendererData.renderData.Count * rendererDatumSize;
                // Renderer fields
                renderersSize += sizeof(byte) + sizeof(uint) + IntPtr.Size;
            }
            // Manager fields
            var directFields = 2 * IntPtr.Size + sizeof(int) + 2 * sizeof(uint);
            var totalSize = renderersSize + directFields;
            ownPlace.Span[0] = new MemorySnapshot("MedusaRendererManager", totalSize, totalSize, memoryBuffer[..childrenCount]);

            var wholeAllocation = 0;

            var children = memoryBuffer[childrenCount..];
            var brgRendererAllocation = _medusaBrgRenderer.GetMemorySnapshot(children, memoryBuffer[..1]);
            wholeAllocation += brgRendererAllocation;

            return wholeAllocation;
        }

        // === EditorAccess
        public readonly struct EditorAccess {
            readonly MedusaRendererManager _manager;

            public MedusaRendererManager Manager => _manager;
            public ref Renderer[] Renderers => ref _manager._renderers;
            public ref int TransformsCount => ref _manager._transformsCount;
            public ref uint AllRenderersCount => ref _manager._allRenderersCount;
            public ref uint AllUvDistributionsCount => ref _manager._allUvDistributionsCount;
            public MedusaBrgRenderer.EditorAccess BrgRenderer => new MedusaBrgRenderer.EditorAccess(_manager._medusaBrgRenderer);

            public EditorAccess(MedusaRendererManager manager) {
                _manager = manager;
            }
        }
    }
}
