using System;
using System.Collections.Generic;
using UnityEngine;

namespace Awaken.Utility.Debugging.MemorySnapshots {
    public interface IMemorySnapshotProvider {
        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace);
    }

    public interface ILeafMemorySnapshotProvider : IMemorySnapshotProvider {
        public void GetMemorySnapshot(Memory<MemorySnapshot> ownPlace);

        int IMemorySnapshotProvider.GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            GetMemorySnapshot(ownPlace);
            return 0;
        }
    }

    public interface INamedLeafMemorySnapshotProvider : ILeafMemorySnapshotProvider {
        public void GetMemorySnapshot(string name, Memory<MemorySnapshot> ownPlace);

        void ILeafMemorySnapshotProvider.GetMemorySnapshot(Memory<MemorySnapshot> ownPlace) {
            GetMemorySnapshot(GetType().Name, ownPlace);
        }
    }

    public interface IMainMemorySnapshotProvider : IMemorySnapshotProvider {
        public static HashSet<IMainMemorySnapshotProvider> Providers { get; } = new HashSet<IMainMemorySnapshotProvider>();

        public int PreallocationSize { get; }

        public static void RegisterProvider(IMainMemorySnapshotProvider provider) {
            Providers.Add(provider);
        }

        public static void UnregisterProvider(IMainMemorySnapshotProvider provider) {
            Providers.Remove(provider);
        }
    }
}
