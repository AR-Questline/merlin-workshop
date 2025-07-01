using System.Threading;
using Unity.Mathematics;

namespace Awaken.PackageUtilities.Threading {
    public struct SpinLock {
        int _lockState;

        public void EnterRead() {
            int s;
            do {
                s = math.max(0, Volatile.Read(ref _lockState));
            } while (Interlocked.CompareExchange(ref _lockState, s + 1, s) != s);
        }

        public void EnterWrite() {
            while (Interlocked.CompareExchange(ref _lockState, -1, 0) != 0) {
            }
        }

        public void ExitRead() {
            Interlocked.Decrement(ref _lockState);
        }

        public void ExitWrite() {
            Interlocked.Exchange(ref _lockState, 0);
        }
    }
}